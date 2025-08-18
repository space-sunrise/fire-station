using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._Scp.Other.Radio;
using Content.Shared.Chat;
using Content.Shared.Emp;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Scp.Other.Radio;

public sealed class ScpRadioSystem : SharedScpRadioSystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRadioComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ScpRadioComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<ScpRadioComponent, RadioReceiveEvent>(OnReceive);

        SubscribeLocalEvent<ScpRadioComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnListen(Entity<ScpRadioComponent> ent, ref ListenEvent args)
    {
        var channel = _prototype.Index(ent.Comp.ActiveChannel);
        _radio.SendRadioMessage(args.Source, args.Message, channel, ent);
    }

    private void OnAttemptListen(Entity<ScpRadioComponent> ent, ref ListenAttemptEvent args)
    {
        if (!ent.Comp.MicrophoneEnabled)
            args.Cancel();

        if (HasComp<EmpDisabledComponent>(ent))
            args.Cancel();
    }

    private void OnReceive(Entity<ScpRadioComponent> ent, ref RadioReceiveEvent args)
    {
        if (!ent.Comp.SpeakerEnabled)
            return;

        if (HasComp<EmpDisabledComponent>(ent))
            return;

        var receiverUid = Transform(ent).ParentUid;

        if (!TryComp<ActorComponent>(receiverUid, out var actor))
        {
            // Если радио не находится у игрока - пусть говорит в чатик.
            SayMessage(ent, args.MessageSource, args.Message);
            return;
        }

        // TODO: Звуки

        _net.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
        if (receiverUid != args.MessageSource && !args.Receivers.Contains(receiverUid))
            args.Receivers.Add(receiverUid);
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

    private void SayMessage(Entity<ScpRadioComponent> ent, EntityUid source, string message)
    {
        var nameEv = new TransformSpeakerNameEvent(source, Name(source));
        RaiseLocalEvent(source, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(ent)),
            ("originalName", nameEv.VoiceName));

        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Whisper, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
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
        if (ent.Comp.SpeakerEnabled)
            EnsureComp<ActiveRadioComponent>(ent).Channels = ent.Comp.Channels.Select(id => id.ToString()).ToHashSet();
        else
            RemCompDeferred<ActiveRadioComponent>(ent);
    }

    private void OnEmpPulse(Entity<ScpRadioComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
    }
}
