using System.Linq;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Protection;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System : EntitySystem
{
	// TODO: SOUNDING, EFFECTS.
    // TODO: Переместить требования к акшенам в прототип акшенов

	[Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
	[Dependency] private readonly SharedPopupSystem _popup = default!;
	[Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    private readonly SoundSpecifier _teleportSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/return.ogg");


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsAction>(OnBackroomsAction);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportAction>(OnRandomTeleportAction);
        SubscribeLocalEvent<Scp106Component, Scp106BecomePhantomAction>(OnScp106BecomePhantomAction);
        SubscribeLocalEvent<Scp106Component, Scp106BecomeTeleportPhantomAction>(OnBecomeTeleportPhantomAction);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsActionEvent>(OnBackroomsDoAfter);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportActionEvent>(OnTeleportDoAfter);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106BecomeTeleportPhantomActionEvent>(OnBecomeTeleportPhantomActionEvent);
        SubscribeLocalEvent<Scp106Component, Scp106CreatePortalAction>(OnScp106CreatePortalAction);
        SubscribeLocalEvent<Scp106Component, Scp106CreatePortalEvent>(OnScp106CreatePortalEvent);

        #region Phantom

        SubscribeLocalEvent<Scp106PhantomComponent, Scp106ReverseAction>(OnScp106ReverseAction);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106LeavePhantomAction>(OnScp106LeavePhantomAction);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106PassThroughAction>(OnScp106PassThroughAction);

        SubscribeLocalEvent<Scp106PhantomComponent, Scp106PassThroughActionEvent>(OnScp106PassThroughActionEvent);

        #endregion
    }

    private void OnScp106CreatePortalEvent(Entity<Scp106Component> ent, ref Scp106CreatePortalEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        Scp106SpawnPortal(ent);

        args.Handled = true;
    }

    private void OnScp106CreatePortalAction(Entity<Scp106Component> ent, ref Scp106CreatePortalAction args)
    {
        if (ent.Comp.Essence < 120)
        {
            _popup.PopupEntity(Loc.GetString("not-enough-essence", ( "count", 120 - ent.Comp.Essence)), ent, PopupType.Medium);
            return;
        }

        if (ent.Comp.Scp106HasPortals >= ent.Comp.MaxScp106Portals)
        {
            _popup.PopupEntity(Loc.GetString("Достигнуто максимальное количество порталов"), ent, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(3), new Scp106CreatePortalEvent(), ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnBecomeTeleportPhantomAction(Entity<Scp106Component> ent, ref Scp106BecomeTeleportPhantomAction args)
    {
        if (ent.Comp.IsContained)
        {
            _popup.PopupEntity("Ваши способности подавлены", ent, ent, PopupType.SmallCaution);
            return;
        }

        var essence = ent.Comp.Essence;

        if (essence < 30)
        {
            _popup.PopupEntity($"Недостаточно {30 - essence} эссенции!", ent, PopupType.Large);
            return;
        }

        ent.Comp.Essence -= 30;
        Dirty(ent);

        BecomeTeleportPhantom(ent);
    }

    private void OnBecomeTeleportPhantomActionEvent(Entity<Scp106PhantomComponent> ent, ref Scp106BecomeTeleportPhantomActionEvent args)
    {
        if (args.Cancelled)
        {
            if (args.Args.EventTarget == null)
                return;

            if (_mind.TryGetMind(args.Args.EventTarget.Value, out var mindId, out _))
            {
                _mind.TransferTo(mindId, args.Args.User);
                _appearance.SetData(args.Args.User, Scp106Visuals.Visuals, Scp106VisualsState.Default);
                _mob.ChangeMobState(args.Args.EventTarget.Value, MobState.Dead);

                return;
            }
        }

        if (PhantomTeleport(args))
            args.Handled = true;
    }

    private void OnScp106BecomePhantomAction(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Essence < 30)
        {
            _popup.PopupEntity($"Недостаточно {30 - ent.Comp.Essence} эссенции!", ent, PopupType.Large);

            return;
        }

        ent.Comp.Essence -= 30;
        Dirty(ent);

        BecomePhantom(ent, ref args);
    }

	private void OnBackroomsAction(Entity<Scp106Component> ent, ref Scp106BackroomsAction args)
    {
        if (args.Handled)
            return;

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

        if (ent.Comp.Essence < 30)
        {
            _popup.PopupEntity($"Недостаточно {30 - ent.Comp.Essence} эссенции!", ent, PopupType.Large);
            return;
        }

        ent.Comp.Essence -= 30;
        Dirty(ent, ent.Comp);

        _stun.TryStun(ent, TimeSpan.FromSeconds(5.5), false);
        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106BackroomsActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = false,
            BreakOnHandChange = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        _appearance.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Entering);

        args.Handled = true;
    }

    private void OnRandomTeleportAction(Entity<Scp106Component> ent, ref Scp106RandomTeleportAction args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.IsContained)
        {
            _popup.PopupEntity("Ваши способности подавлены", ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Essence < 30)
        {
            _popup.PopupEntity($"Недостаточно {30 - ent.Comp.Essence} эссенции!", ent, PopupType.Large);
            return;
        }

        ent.Comp.Essence -= 30;
        Dirty(ent, ent.Comp);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106RandomTeleportActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };

        _stun.TryStun(ent, TimeSpan.FromSeconds(5.5), false);
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        _appearance.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Entering);

        args.Handled = true;
    }

	private void OnBackroomsDoAfter(Entity<Scp106Component> ent, ref Scp106BackroomsActionEvent args)
	{
        if (args.Cancelled)
            return;


        DoTeleportEffects(ent);
        SendToBackrooms(ent);
    }

	private void OnTeleportDoAfter(Entity<Scp106Component> ent, ref Scp106RandomTeleportActionEvent args)
    {
        if (args.Cancelled)
            return;

        DoTeleportEffects(ent);
        SendToStation(ent);
    }

    private void DoTeleportEffects(EntityUid uid)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _audio.PlayEntity(_teleportSound, uid, uid);
    }

    private void OnMeleeHit(Entity<Scp106Component> ent, ref MeleeHitEvent args)
    {
        if (TryComp<Scp106BackRoomMapComponent>(Transform(ent).MapUid, out _))
        {
            args.BonusDamage = args.BaseDamage * 3;
        }

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

            SendToBackrooms(entity, ent);
        }
    }

    public virtual async void SendToBackrooms(EntityUid target, Entity<Scp106Component>? scp106 = null) {}

    public abstract void SendToStation(EntityUid target);

    // TODO: Реализовать
    public virtual void SendToHuman(EntityUid target) {}

    public virtual bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args) { return false; }

    public abstract void Scp106FinishTeleportation(EntityUid uid);

    public abstract void BecomeTeleportPhantom(EntityUid uid);

    public abstract void BecomePhantom(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args);

    public abstract void Scp106SpawnPortal(Entity<Scp106Component> ent);
}

[NetSerializable, Serializable]
public enum Scp106VisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}
