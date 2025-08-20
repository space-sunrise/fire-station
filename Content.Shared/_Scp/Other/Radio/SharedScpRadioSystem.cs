using System.Linq;
using Content.Shared._Scp.Other.Events;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Other.Radio;

// TODO: Локализация
public abstract class SharedScpRadioSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRadioComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ScpRadioComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<ScpRadioComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ScpRadioComponent, GotEquippedEvent>(DisableAmbience);
        SubscribeLocalEvent<ScpRadioComponent, EntityInsertedIntoStorageEvent>(DisableAmbience);
        SubscribeLocalEvent<ScpRadioComponent, GotUnequippedEvent>(EnableAmbience);
        SubscribeLocalEvent<ScpRadioComponent, EntityRemovedFromStorageEvent>(EnableAmbience);
    }

    protected virtual void OnStartup(Entity<ScpRadioComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ActiveChannel = ent.Comp.Channels.First();
    }

    private void OnActivate(Entity<ScpRadioComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        ToggleMicrophone(ent, args.User);

        args.Handled = true;
    }

    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!ev.CanInteract || !ev.CanComplexInteract)
            return;

        if (!TryComp<ScpRadioComponent>(ev.Target, out var scpRadio))
            return;

        AddCycleChannelVerb((ev.Target, scpRadio), ev);
        AddToggleSpeakerVerb((ev.Target, scpRadio), ev);
    }

    private void OnExamine(Entity<ScpRadioComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = PrototypeManager.Index(ent.Comp.ActiveChannel);

        using (args.PushGroup(nameof(ScpRadioComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void DisableAmbience<T>(Entity<ScpRadioComponent> ent, ref T _)
    {
        _ambientSound.SetAmbience(ent, false);
    }

    private void EnableAmbience<T>(Entity<ScpRadioComponent> ent, ref T _)
    {
        if (!ent.Comp.Enabled)
            return;

        _ambientSound.SetAmbience(ent, true);
    }

    private void AddCycleChannelVerb(Entity<ScpRadioComponent> ent, GetVerbsEvent<Verb> ev)
    {
        if (ent.Comp.Channels.Count <= 1)
            return;

        var verb = new Verb
        {
            Text = Loc.GetString("scp-radio-cycle-channel"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () =>
            {
                CycleChannel(ent, ev.User);
            },
        };

        ev.Verbs.Add(verb);
    }

    private void AddToggleSpeakerVerb(Entity<ScpRadioComponent> ent, GetVerbsEvent<Verb> ev)
    {
        var verb = new Verb
        {
            Text = Loc.GetString("scp-radio-toggle-radio"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () =>
            {
                ToggleRadio(ent, !ent.Comp.Enabled, ev.User);
            },
        };

        ev.Verbs.Add(verb);
    }

    private void CycleChannel(Entity<ScpRadioComponent> ent, EntityUid user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var next = GetNextChannel(ent.Comp.Channels, ent.Comp.ActiveChannel);

        if (next == ent.Comp.ActiveChannel)
            return;

        if (!PrototypeManager.TryIndex(next, out var nextPrototype))
            return;

        ent.Comp.ActiveChannel = next;

        var message = Loc.GetString("scp-radio-current-channel", ("name", nextPrototype.LocalizedName));
        _popup.PopupPredicted(message, ent, user);
        _audio.PlayLocal(ent.Comp.ChannelCycleSound, user, ent);
    }

    protected virtual void ToggleMicrophone(Entity<ScpRadioComponent> ent, EntityUid user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.MicrophoneEnabled = !ent.Comp.MicrophoneEnabled;

        var message = Loc.GetString("scp-radio-microphone", ("value", ent.Comp.MicrophoneEnabled));
        _popup.PopupPredicted(message, ent, user);
        _audio.PlayLocal(ent.Comp.ToggleSound, user, ent);
    }

    protected virtual void ToggleRadio(Entity<ScpRadioComponent> ent, bool value, EntityUid? user = null) { }

    private static ProtoId<RadioChannelPrototype> GetNextChannel(List<ProtoId<RadioChannelPrototype>> channels,
        ProtoId<RadioChannelPrototype> current)
    {
        var count = channels.Count;
        var index = channels.IndexOf(current);
        var nextIndex = index + 1;

        if (nextIndex >= count)
            return channels[0];

        return channels[nextIndex];
    }
}
