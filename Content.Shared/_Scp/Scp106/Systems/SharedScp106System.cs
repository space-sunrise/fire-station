using System.Linq;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Protection;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
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
	[Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
	[Dependency] private readonly SharedPopupSystem _popup = default!;
	[Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/return.ogg");

    private const float DamageInPocketDimensionMultiplier = 3f;
    protected static readonly TimeSpan TeleportTimeCompensation = TimeSpan.FromSeconds(0.1f);

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

        #region Phantom

        SubscribeLocalEvent<Scp106PhantomComponent, Scp106ReverseAction>(OnScp106ReverseAction);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106LeavePhantomAction>(OnScp106LeavePhantomAction);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106PassThroughAction>(OnScp106PassThroughAction);

        SubscribeLocalEvent<Scp106PhantomComponent, Scp106PassThroughActionEvent>(OnScp106PassThroughActionEvent);

        #endregion
    }

    private void OnBecomeTeleportPhantomAction(Entity<Scp106Component> ent, ref Scp106BecomeTeleportPhantomAction args)
    {
        if (CheckIsContained(ent))
            return;

        if (!TryDeductEssence(ent, args.Cost))
            return;

        BecomeTeleportPhantom(ent, ref args);
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

        if (!TryDeductEssence(ent, args.Cost))
            return;

        BecomePhantom(ent, ref args);
    }

	private void OnBackroomsAction(Entity<Scp106Component> ent, ref Scp106BackroomsAction args)
    {
        if (CheckIsInDimension(ent))
        {
            _popup.PopupEntity("Вы уже в своем измерении", ent, ent, PopupType.SmallCaution);
            return;
        }

        TryDoTeleport(ent, ref args, new Scp106BackroomsActionEvent ());
    }

    private void OnRandomTeleportAction(Entity<Scp106Component> ent, ref Scp106RandomTeleportAction args)
    {
        TryDoTeleport(ent, ref args, new Scp106RandomTeleportActionEvent ());
    }

    private bool TryDoTeleport<T>(Entity<Scp106Component> ent, ref T args, SimpleDoAfterEvent doAfterEvent) where T : Scp106ValuableActionEvent
    {
        if (args.Handled)
            return false;

        if (CheckIsContained(ent))
            return false;

        if (!TryDeductEssence(ent, args.Cost))
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, ent.Comp.TeleportationDuration, doAfterEvent, args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);

        _stun.TryStun(ent, ent.Comp.TeleportationDuration + TeleportTimeCompensation, true);
        _appearance.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Entering);

        args.Handled = true;
        return true;
    }

	private void OnBackroomsDoAfter(Entity<Scp106Component> ent, ref Scp106BackroomsActionEvent args)
	{
        if (args.Cancelled || args.Handled)
            return;

        DoTeleportEffects(ent);
        SendToBackrooms(ent);

        args.Handled = true;
    }

	private void OnTeleportDoAfter(Entity<Scp106Component> ent, ref Scp106RandomTeleportActionEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        DoTeleportEffects(ent);
        SendToStation(ent);

        args.Handled = true;
    }

    private void OnMeleeHit(Entity<Scp106Component> ent, ref MeleeHitEvent args)
    {
        if (TryComp<Scp106BackRoomMapComponent>(Transform(ent).MapUid, out _))
        {
            args.BonusDamage = args.BaseDamage * DamageInPocketDimensionMultiplier;
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

    #region Helpers

    private void DoTeleportEffects(EntityUid uid)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _audio.PlayEntity(TeleportSound, uid, uid);
    }

    public bool TryDeductEssence(Entity<Scp106Component> ent, FixedPoint2 cost)
    {
        if (ent.Comp.Essence < cost)
        {
            var message = Loc.GetString("not-enough-essence", ("count", cost - ent.Comp.Essence));
            _popup.PopupClient(message, ent, ent, PopupType.Medium);

            return false;
        }

        ent.Comp.Essence -= cost;
        Dirty(ent);

        return true;
    }

    public bool CheckIsContained(Entity<Scp106Component> ent)
    {
        if (ent.Comp.IsContained)
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupClient("Ваши способности подавлены", ent, ent,  PopupType.SmallCaution);

            return true;
        }

        return false;
    }

    public bool CheckIsInDimension(EntityUid ent)
    {
        var mapUid = Transform(ent).MapUid;

        return HasComp<Scp106BackRoomMapComponent>(mapUid);
    }

    #endregion

    public virtual async void SendToBackrooms(EntityUid target, Entity<Scp106Component>? scp106 = null) {}

    public virtual void SendToStation(EntityUid target) {}

    // TODO: Реализовать
    public virtual void SendToHuman(EntityUid target) {}

    public virtual bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args) { return false; }

    public virtual void BecomeTeleportPhantom(EntityUid uid, ref Scp106BecomeTeleportPhantomAction args) {}

    public virtual void BecomePhantom(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args) {}
}

[NetSerializable, Serializable]
public enum Scp106VisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}
