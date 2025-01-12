using System.Linq;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Protection;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract class SharedScp106System : EntitySystem
{
	// TODO: SOUNDING, EFFECTS.

	[Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
	[Dependency] private readonly SharedPopupSystem _popup = default!;
	[Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedTransformSystem _sharedTransform = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IServerNetManager _serverNetManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SoundSpecifier _teleportSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/return.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsAction>(OnBackroomsAction);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportAction>(OnRandomTeleportAction);
        SubscribeLocalEvent<Scp106Component, Scp106BecomePhantomAction>(OnScp106BecomePhantomAction);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsActionEvent>(OnBackroomsDoAfter);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportActionEvent>(OnTeleportDoAfter);

        // Phantom
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106ReverseAction>(OnScp106ReverseAction);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106LeavePhantomAction>(OnScp106LeavePhantomAction);

    }

    private void OnScp106LeavePhantomAction(EntityUid uid,
        Scp106PhantomComponent component,
        Scp106LeavePhantomAction args)
    {
        RemComp<Scp106PhantomComponent>(uid);
    }

    private void OnScp106ReverseAction(EntityUid uid, Scp106PhantomComponent component, Scp106ReverseAction args)
    {
        var target = args.Target;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!TryComp<MobStateComponent>(target, out var mobStateComponent))
            return;

        if (mobStateComponent.CurrentState != MobState.Dead)
            return;

        var targetPos = Transform(target).Coordinates;

        _sharedTransform.SetCoordinates(component.Scp106BodyUid, targetPos);
        _bodySystem.GibBody(target);

        RemComp<Scp106PhantomComponent>(uid);
    }

    private void OnScp106BecomePhantomAction(EntityUid uid, Scp106Component component, Scp106BecomePhantomAction args)
    {
        if (component.AmoutOfPhantoms <= 0)
        {
            var time = 180 - component.Accumulator;
            _popup.PopupEntity($"У вас закончились фантомы!\nПодождите {Math.Floor(time)} сек", uid, uid, PopupType.Large);
            return;
        }

        if (!_serverNetManager.IsServer)
            return;

        var pos = Transform(uid).Coordinates;

        var scp106Phantom = Spawn("Scp106Phantom", pos);

        if (_mindSystem.TryGetMind(uid, out var mindId, out _))
        {
            _mindSystem.TransferTo(mindId, scp106Phantom);
            component.AmoutOfPhantoms -= 1;
            Dirty(uid, component);
        }
        if (!TryComp<Scp106PhantomComponent>(scp106Phantom, out var scp106PhantomComponent))
            return;

        scp106PhantomComponent.Scp106BodyUid = uid;
        Dirty(uid, component);
    }

	private void OnBackroomsAction(Entity<Scp106Component> ent, ref Scp106BackroomsAction args)
	{
        if (ent.Comp.IsContained)
        {
            _popup.PopupEntity("Ваши способности подавлены", ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (HasComp<Scp106BackRoomMapComponent>(Transform(ent).MapUid))
        {
            _popup.PopupEntity("Вы уже в своем измерении", ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106BackroomsActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        _appearanceSystem.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Entering);
    }

    private void OnRandomTeleportAction(Entity<Scp106Component> ent, ref Scp106RandomTeleportAction args)
    {
        if (ent.Comp.IsContained)
        {
            _popup.PopupEntity("Ваши способности подавлены", ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106RandomTeleportActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        _appearanceSystem.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Entering);
    }

	private void OnBackroomsDoAfter(Entity<Scp106Component> ent, ref Scp106BackroomsActionEvent args)
	{
        DoTeleportEffects(ent);

		if (args.Cancelled)
			return;

        SendToBackrooms(args.User);
    }

	private void OnTeleportDoAfter(Entity<Scp106Component> ent, ref Scp106RandomTeleportActionEvent args)
    {
        DoTeleportEffects(ent);

		if (args.Cancelled)
			return;

        SendToStation(ent);
    }

    private void DoTeleportEffects(EntityUid uid)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _appearanceSystem.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Default);
        _audio.PlayEntity(_teleportSound, uid, uid);
    }

    private void OnMeleeHit(Entity<Scp106Component> ent, ref MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!args.IsHit || !args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
            if (entity == ent.Owner)
                return;

            if (HasComp<Scp106ProtectionComponent>(entity))
                continue;

            SendToBackrooms(entity);
        }
    }

    public virtual async void SendToBackrooms(EntityUid target) {}

    public virtual void SendToStation(EntityUid target) {}

    public virtual void SendToHuman(EntityUid target) {}
}
