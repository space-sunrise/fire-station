using Content.Shared._Scp.Scp939;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Pulling.Events;

namespace Content.Shared._Scp.Scp939Mask;

public sealed class Scp939MaskSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp939MaskComponent, BeingEquippedAttemptEvent>(OnEquippeAttempt);
        SubscribeLocalEvent<Scp939MaskComponent, ClothingGotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<Scp939MaskComponent, ClothingGotUnequippedEvent>(OnMaskUnequipped);


        SubscribeLocalEvent<Scp939MaskUserComponent, AttemptStopPullingEvent>(HandleStopPull);
        SubscribeLocalEvent<Scp939MaskUserComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
        SubscribeLocalEvent<Scp939MaskUserComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<Scp939MaskUserComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<Scp939MaskUserComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        SubscribeLocalEvent<Scp939MaskUserComponent, BuckleAttemptEvent>(OnBuckleAttemptEvent);
        SubscribeLocalEvent<Scp939MaskUserComponent, UnbuckleAttemptEvent>(OnUnbuckleAttemptEvent);
        SubscribeLocalEvent<Scp939MaskUserComponent, PullStartedMessage>(OnPull);
        SubscribeLocalEvent<Scp939MaskUserComponent, PullStoppedMessage>(OnPull);

        SubscribeLocalEvent<Scp939MaskUserComponent, DropAttemptEvent>(CheckAct);
        SubscribeLocalEvent<Scp939MaskUserComponent, PickupAttemptEvent>(CheckAct);
        SubscribeLocalEvent<Scp939MaskUserComponent, AttackAttemptEvent>(CheckAct);
        SubscribeLocalEvent<Scp939MaskUserComponent, UseAttemptEvent>(CheckAct);
        SubscribeLocalEvent<Scp939MaskUserComponent, InteractionAttemptEvent>(CheckInteract);
    }

    private void OnBeingPulledAttempt(EntityUid uid, Scp939MaskUserComponent component, BeingPulledAttemptEvent args)
    {
        if (!TryComp<PullableComponent>(uid, out var pullable))
            return;

        if (pullable.Puller != null )
            args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, Scp939MaskUserComponent component, IsEquippingAttemptEvent args)
    {
        if (args.Equipee == uid)
            CheckAct(uid, component, args);
    }

    private void OnUnequipAttempt(EntityUid uid, Scp939MaskUserComponent component, IsUnequippingAttemptEvent args)
    {
        if (args.Unequipee == uid)
            CheckAct(uid, component, args);
    }

    private void HandleMoveAttempt(EntityUid uid, Scp939MaskUserComponent component, UpdateCanMoveEvent args)
    {
        if (!TryComp<PullableComponent>(uid, out var pullable))
            return;

        if (pullable.BeingPulled)
        {
            args.Cancel();
        }
    }

    private void HandleStopPull(EntityUid uid, Scp939MaskUserComponent component, AttemptStopPullingEvent args)
    {
        if (args.User == null || !Exists(args.User.Value))
            return;

        args.Cancelled = true;
    }

    private void OnBuckleAttemptEvent(Entity<Scp939MaskUserComponent> ent, ref BuckleAttemptEvent args)
    {
        OnBuckleAttempt(ent, args.User, ref args.Cancelled);
    }

    private void OnUnbuckleAttemptEvent(Entity<Scp939MaskUserComponent> ent, ref UnbuckleAttemptEvent args)
    {
        OnBuckleAttempt(ent, args.User, ref args.Cancelled);
    }

    private void OnBuckleAttempt(Entity<Scp939MaskUserComponent> ent, EntityUid? user, ref bool cancelled)
    {
        if (cancelled || user != ent.Owner)
            return;

        cancelled = true;
    }

    private void OnPull(EntityUid uid, Scp939MaskUserComponent component, PullMessage args)
       => _actionBlocker.UpdateCanMove(uid);

    private void CheckAct(EntityUid uid, Scp939MaskUserComponent component, CancellableEntityEventArgs args)
        => args.Cancel();

    private void CheckInteract(Entity<Scp939MaskUserComponent> ent, ref InteractionAttemptEvent args)
       => args.Cancelled = true;

    private void OnEquippeAttempt(EntityUid uid, Scp939MaskComponent component, BeingEquippedAttemptEvent args)
    {
        if (!HasComp<Scp939Component>(args.EquipTarget))
            args.Cancel();
    }

    private void OnMaskEquipped(EntityUid uid, Scp939MaskComponent component, ClothingGotEquippedEvent args)
    {
        component.User = args.Wearer;
        Dirty(uid, component);

        var a = EnsureComp<Scp939MaskUserComponent>(args.Wearer);
        a.Mask = uid;
        Dirty(args.Wearer, a);
    }

    private void OnMaskUnequipped(EntityUid uid, Scp939MaskComponent component, ClothingGotUnequippedEvent args)
    {
        RemComp<Scp939MaskUserComponent>(args.Wearer);
        component.User = null;
        Dirty(uid, component);

        _actionBlocker.UpdateCanMove(args.Wearer);
    }
}
