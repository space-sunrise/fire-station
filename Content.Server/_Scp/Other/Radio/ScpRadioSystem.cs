using System.Linq;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._Scp.Other.Radio;
using Content.Shared.Chat;
using Content.Shared.Emp;
using Content.Shared.PowerCell.Components;
using Robust.Server.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Scp.Other.Radio;

public sealed class ScpRadioSystem : SharedScpRadioSystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRadioComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ScpRadioComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<ScpRadioComponent, RadioReceiveEvent>(OnReceive);
        SubscribeLocalEvent<ScpRadioComponent, RadioReceiveAttemptEvent>(OnAttemptReceive);

        SubscribeLocalEvent<ScpRadioComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<ScpRadioComponent, EmpPulseEvent>(OnEmpPulse);

        _sawmill = _log.GetSawmill("scp_radio");
    }

    private void OnListen(Entity<ScpRadioComponent> ent, ref ListenEvent args)
    {
        var channel = PrototypeManager.Index(ent.Comp.ActiveChannel);
        _radio.SendRadioMessage(args.Source, args.Message, channel, ent);
        _audio.PlayEntity(ent.Comp.SendSound, args.Source, ent);

        // Это находится здесь, а не в начале, чтобы при малом заряде рация проиграла сообщения и сдохла.
        // Вместо того чтобы половина сообщений просто пропускалась из-за малого заряда.
        TryTakeCharge(ent, true);
    }

    private void OnAttemptListen(Entity<ScpRadioComponent> ent, ref ListenAttemptEvent args)
    {
        if (!ent.Comp.Enabled)
            args.Cancel();

        if (!ent.Comp.MicrophoneEnabled)
            args.Cancel();

        if (HasComp<EmpDisabledComponent>(ent))
            args.Cancel();
    }

    private void OnReceive(Entity<ScpRadioComponent> ent, ref RadioReceiveEvent args)
    {
        var receiverUid = Transform(ent).ParentUid;

        if (!TryComp<ActorComponent>(receiverUid, out var actor))
        {
            // Если радио не находится у игрока - пусть говорит в чатик.
            SayMessage(ent, args.MessageSource, args.Message);
            _audio.PlayPvs(ent.Comp.ReceiveSound, ent);

            return;
        }

        if (args.MessageSource != receiverUid)
            _audio.PlayEntity(ent.Comp.ReceiveSound, receiverUid, ent);

        _net.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
        if (receiverUid != args.MessageSource && !args.Receivers.Contains(receiverUid))
            args.Receivers.Add(receiverUid);

        // Это находится здесь, а не в начале, чтобы при малом заряде рация проиграла сообщения и сдохла.
        // Вместо того чтобы половина сообщений просто пропускалась из-за малого заряда.
        TryTakeCharge(ent);
    }

    private void OnAttemptReceive(Entity<ScpRadioComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (!ent.Comp.Enabled)
            args.Cancelled = true;

        if (HasComp<EmpDisabledComponent>(ent))
            args.Cancelled = true;
    }

    protected override void ToggleMicrophone(Entity<ScpRadioComponent> ent, EntityUid user)
    {
        base.ToggleMicrophone(ent, user);

        UpdateMicrophone(ent);
    }

    protected override void OnStartup(Entity<ScpRadioComponent> ent, ref ComponentStartup args)
    {
        base.OnStartup(ent, ref args);

        UpdateMicrophone(ent);
        UpdateSpeaker(ent);
    }

    private bool TryTakeCharge(Entity<ScpRadioComponent> ent, bool sending = false)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var batteryUid, out var battery))
        {
            ToggleRadio(ent, false);
            return false;
        }

        var wattage = sending ? ent.Comp.WattageSendMessage : ent.Comp.WattageReceiveMessage;

        if (!_battery.TryUseCharge(batteryUid.Value, wattage, battery))
        {
            ToggleRadio(ent, false);

            // Дообъедаем остаток заряда, чтобы избежать проблем с минимальными остатками типа 0.0001%
            _battery.TryUseCharge(batteryUid.Value, battery.CurrentCharge, battery);

            return false;
        }

        return true;
    }

    private void SayMessage(Entity<ScpRadioComponent> ent, EntityUid source, string message)
    {
        var nameEv = new TransformSpeakerNameEvent(source, Name(source));
        RaiseLocalEvent(source, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(ent)),
            ("originalName", nameEv.VoiceName));

        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Whisper, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
    }

    protected override void ToggleRadio(Entity<ScpRadioComponent> ent, bool value, EntityUid? user = null)
    {
        base.ToggleRadio(ent, value, user);

        user ??= Transform(ent).ParentUid;

        // Вдруг, по какой-то случайности, ParentUid будет не существовать вообще.
        if (!Exists(user))
        {
            _sawmill.Error("Found non-existing user while toggling radio");
            return;
        }

        // Если мы включаем(value == true) и недостаточно заряда -> выходим из метода
        // Если выключаем(value == false) -> проверка на заряд не нужна, просто выключаем.
        if ((!_powerCell.TryGetBatteryFromSlot(ent, out _, out var battery) || MathHelper.CloseTo(battery.CurrentCharge, 0f))
            && value)
        {
            var failMessage = Loc.GetString("scp-radio-not-enough-charge");
            _popup.PopupEntity(failMessage, ent, user.Value);

            return;
        }

        ent.Comp.Enabled = value;
        Dirty(ent);

        var message = Loc.GetString("scp-radio-toggle-message", ("name", Name(ent)), ("value", ent.Comp.Enabled));

        _popup.PopupEntity(message, ent, user.Value);
        _ambientSound.SetAmbience(ent, value);
        _audio.PlayEntity(ent.Comp.ToggleSound, user.Value, ent);

        UpdateMicrophone(ent);
        UpdateSpeaker(ent);
    }

    private void UpdateMicrophone(Entity<ScpRadioComponent> ent)
    {
        if (ent.Comp.MicrophoneEnabled)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }

    private void UpdateSpeaker(Entity<ScpRadioComponent> ent)
    {
        if (ent.Comp.Enabled)
            EnsureComp<ActiveRadioComponent>(ent).Channels = ent.Comp.Channels.Select(id => id.ToString()).ToHashSet();
        else
            RemCompDeferred<ActiveRadioComponent>(ent);
    }

    private void OnPowerCellChanged(Entity<ScpRadioComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (args.Ejected)
            ToggleRadio(ent, false);
    }

    private void OnEmpPulse(Entity<ScpRadioComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
    }
}
