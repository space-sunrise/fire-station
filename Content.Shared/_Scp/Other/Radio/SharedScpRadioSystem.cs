using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Other.Radio;

// TODO: Локализация
public abstract class SharedScpRadioSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRadioComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ScpRadioComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<ScpRadioComponent, ExaminedEvent>(OnExamine);
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

        var verb = new Verb
        {
            Text = "Переключить канал связи",
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () =>
            {
                CycleChannel((ev.Target, scpRadio), ev.User);
            },
        };

        ev.Verbs.Add(verb);
    }

    private void OnExamine(Entity<ScpRadioComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _prototype.Index(ent.Comp.ActiveChannel);

        using (args.PushGroup(nameof(ScpRadioComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void CycleChannel(Entity<ScpRadioComponent> ent, EntityUid user)
    {
        var next = GetNextChannel(ent.Comp.Channels, ent.Comp.ActiveChannel);

        if (next == ent.Comp.ActiveChannel)
            return;

        if (!_prototype.TryIndex(next, out var nextPrototype))
            return;

        ent.Comp.ActiveChannel = next;

        var message = $"Текущий канал теперь: {nextPrototype.LocalizedName}";
        _popup.PopupPredicted(message, ent, user);

        // TODO: Звук
    }

    protected virtual void ToggleMicrophone(Entity<ScpRadioComponent> ent, EntityUid user)
    {
        ent.Comp.MicrophoneEnabled = !ent.Comp.MicrophoneEnabled;

        var message = ent.Comp.MicrophoneEnabled
            ? "Микрофон включен"
            : "Микрофон выключен";
        _popup.PopupPredicted(message, ent, user);

        // TODO: Звук
    }

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
