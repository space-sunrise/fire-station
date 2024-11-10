using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._Scp.Scp035;

public abstract class SharedScp035System : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp035MaskComponent, ClothingGotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<Scp035MaskComponent, ClothingGotUnequippedEvent>(OnMaskUnequipped);
        SubscribeLocalEvent<Scp035MaskComponent, BeingEquippedAttemptEvent>(OnEquippeAttempt);
        SubscribeLocalEvent((Entity<Scp035MaskComponent> _, ref BeforeDamageChangedEvent args) => args.Cancelled = true);

        SubscribeLocalEvent<Scp035MaskUserComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<Scp035MaskUserComponent, MaskStunActionEvent>(OnStun);
        SubscribeLocalEvent<Scp035MaskUserComponent, MaskOrderActionEvent>(OnOrder);
        SubscribeLocalEvent<Scp035MaskUserComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp035MaskUserComponent, ComponentShutdown>(OnMaskShutdown);

        SubscribeLocalEvent<Scp035ServantComponent, ComponentShutdown>(OnServantShutdown);
    }

    // TODO: Рефактор с целью анхардкода всяких значений в прототипы, перевод строк на EntProtoId, а переводов на локаль
    // TODO: А еще чето с акшенами придумать, не очень смотрятся эти AddAction x6

    private void OnMaskEquipped(Entity<Scp035MaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<UnremoveableComponent>(ent);

        ent.Comp.User = args.Wearer;
        Dirty(ent);

        var maskUserComponent = EnsureComp<Scp035MaskUserComponent>(args.Wearer);
        maskUserComponent.Mask = ent;

        _action.AddAction(args.Wearer, "ActionScp035RaiseArmy", maskUserComponent.ActionRaiseArmy);
        _action.AddAction(args.Wearer, "ActionScp035OrderStay", maskUserComponent.ActionOrderStayEntity);
        _action.AddAction(args.Wearer, "ActionScp035OrderFollow", maskUserComponent.ActionOrderFollowEntity);
        _action.AddAction(args.Wearer, "ActionScp035OrderKill", maskUserComponent.ActionOrderKillEmEntity);
        _action.AddAction(args.Wearer, "ActionScp035OrderLoose", maskUserComponent.ActionOrderLooseEntity);
        _action.AddAction(args.Wearer, "ActionScp035Stun", maskUserComponent.ActionStunEntity);
        Dirty(args.Wearer, maskUserComponent);

        _factionSystem.ClearFactions(args.Wearer);
        _factionSystem.AddFaction(args.Wearer, "SimpleHostile");

        _mobThreshold.SetMobStateThreshold(args.Wearer, FixedPoint2.New(800), MobState.Critical);
        _mobThreshold.SetMobStateThreshold(args.Wearer, FixedPoint2.New(800), MobState.Dead);
        RemComp<SlowOnDamageComponent>(args.Wearer);

        _stun.TryParalyze(args.Wearer, TimeSpan.FromSeconds(5), true);

        if (_net.IsServer)
            _popup.PopupEntity("Вы ошеломлены!", args.Wearer, args.Wearer, PopupType.LargeCaution);

        var chainsaw = Spawn("Chainsaw", Transform(args.Wearer).Coordinates);
        _hands.TryForcePickupAnyHand(args.Wearer, chainsaw, false);
    }

    private void OnMaskUnequipped(Entity<Scp035MaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
        Dirty(ent);

        RemComp<Scp035MaskUserComponent>(args.Wearer);
    }

    private void OnEquippeAttempt(Entity<Scp035MaskComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Equipee))
        {
            args.Cancel();

            _stun.TryParalyze(args.Equipee, TimeSpan.FromSeconds(10), true);

            if (_net.IsServer)
            {
                _popup.PopupEntity("Маска отвергает вас!", args.Equipee, args.Equipee, PopupType.LargeCaution);

                var impulse = _random.NextVector2() * 10000;
                _physics.ApplyLinearImpulse(args.Equipee, impulse);
            }
        }
    }

    private void OnMobStateChanged(Entity<Scp035MaskUserComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!ent.Comp.Mask.HasValue)
            return;

        var maskEntity = ent.Comp.Mask.Value;

        RemComp<UnremoveableComponent>(maskEntity);
        _containerSystem.TryRemoveFromContainer(maskEntity, true);
        _transform.AttachToGridOrMap(maskEntity);

        var ash = Spawn("Ash", Transform(maskEntity).Coordinates);
        _transform.AttachToGridOrMap(ash);

        QueueDel(ent);
    }

    private void OnMeleeHit(Entity<Scp035MaskUserComponent> ent, ref MeleeHitEvent args)
    {
        args.BonusDamage = args.BaseDamage * 4;
    }

    private void OnStun(Entity<Scp035MaskUserComponent> ent, ref MaskStunActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            if(_net.IsServer)
                _popup.PopupEntity("Работает только на людей.", args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(10), true);

        if (_net.IsServer)
            _popup.PopupEntity("ваше тело онемело!", args.Target, args.Target, PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnMaskShutdown(Entity<Scp035MaskUserComponent> ent, ref ComponentShutdown args)
    {
        foreach (var servant in ent.Comp.Servants)
        {
            if (TryComp(servant, out Scp035ServantComponent? servantComp))
                servantComp.User = null;

            _mobState.ChangeMobState(servant, MobState.Dead);
        }

        _action.RemoveAction(ent, ent.Comp.ActionRaiseArmy);
        _action.RemoveAction(ent, ent.Comp.ActionOrderStayEntity);
        _action.RemoveAction(ent, ent.Comp.ActionOrderFollowEntity);
        _action.RemoveAction(ent, ent.Comp.ActionOrderKillEmEntity);
        _action.RemoveAction(ent, ent.Comp.ActionOrderLooseEntity);
        _action.RemoveAction(ent, ent.Comp.ActionStunEntity);
    }

    private void OnServantShutdown(Entity<Scp035ServantComponent> ent, ref ComponentShutdown args)
    {
        var mask = ent.Comp.User;
        if (TryComp<Scp035MaskUserComponent>(mask, out var maskUserComponent))
        {
            maskUserComponent.Servants.Remove(ent);
        }
    }

    private void OnOrder(Entity<Scp035MaskUserComponent> ent, ref MaskOrderActionEvent args)
    {
        if (ent.Comp.CurrentOrder == args.Type)
            return;

        args.Handled = true;

        ent.Comp.CurrentOrder = args.Type;
        Dirty(ent);

        UpdateActions(ent.Owner, ent.Comp);
        UpdateAllServants(ent.Owner, ent.Comp);
    }

    private void UpdateActions(EntityUid uid, Scp035MaskUserComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderStayEntity, component.CurrentOrder == MaskOrderType.Stay);
        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == MaskOrderType.Follow);
        _action.SetToggled(component.ActionOrderKillEmEntity, component.CurrentOrder == MaskOrderType.Kill);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == MaskOrderType.Loose);

        _action.StartUseDelay(component.ActionOrderStayEntity);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderKillEmEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }

    public void UpdateAllServants(EntityUid uid, Scp035MaskUserComponent component)
    {
        foreach (var servant in component.Servants)
        {
            UpdateServantNpc(servant, component.CurrentOrder);
        }
    }

    public virtual void UpdateServantNpc(EntityUid uid, MaskOrderType orderType)
    {
    }
}
