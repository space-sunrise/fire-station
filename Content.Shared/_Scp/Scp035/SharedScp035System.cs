using System.Linq;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Scp.Scp035;

// TODO: Придумать что-то с акшенами, не очень смотрятся эти AddAction x6
public abstract class SharedScp035System : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp035MaskComponent, ClothingGotEquippedEvent>(OnMaskEquipped);
        SubscribeLocalEvent<Scp035MaskComponent, ClothingGotUnequippedEvent>(OnMaskUnequipped);
        SubscribeLocalEvent<Scp035MaskComponent, BeingEquippedAttemptEvent>(OnEquippeAttempt);

        SubscribeLocalEvent<Scp035MaskUserComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<Scp035MaskUserComponent, MaskStunActionEvent>(OnStun);
        SubscribeLocalEvent<Scp035MaskUserComponent, MaskOrderActionEvent>(OnOrder);
        SubscribeLocalEvent<Scp035MaskUserComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp035MaskUserComponent, ComponentStartup>(OnMaskUserStartUp);
        SubscribeLocalEvent<Scp035MaskUserComponent, ComponentShutdown>(OnMaskShutdown);

        SubscribeLocalEvent<Scp035ServantComponent, ComponentShutdown>(OnServantShutdown);
    }

    protected virtual void OnMaskEquipped(Entity<Scp035MaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<UnremoveableComponent>(ent);

        ent.Comp.User = args.Wearer;
        Dirty(ent);

        var maskUserComponent = EnsureComp<Scp035MaskUserComponent>(args.Wearer);
        maskUserComponent.Mask = ent;

        ToggleActions(maskUserComponent, ent.Comp, ent, true);
        Dirty(args.Wearer, maskUserComponent);

        _faction.ClearFactions(args.Wearer);
        _faction.AddFaction(args.Wearer, ent.Comp.NewUserFaction);

        _mobThreshold.SetMobStateThreshold(args.Wearer, ent.Comp.NewCriticalThreshold, MobState.Critical);
        _mobThreshold.SetMobStateThreshold(args.Wearer, ent.Comp.NewDeadThreshold, MobState.Dead);
        RemComp<SlowOnDamageComponent>(args.Wearer);

        _stun.TryAddParalyzeDuration(args.Wearer, ent.Comp.EquippedParalyzeDuration);

        _popup.PopupClient(Loc.GetString("scp-035-paralyze-effect"), args.Wearer, args.Wearer, PopupType.LargeCaution);
        _audio.PlayEntity(ent.Comp.EquipSound, args.Wearer, args.Wearer);

        var weapon = Spawn(ent.Comp.SpawnWeaponProto, Transform(args.Wearer).Coordinates);
        _hands.TryForcePickupAnyHand(args.Wearer, weapon, false);

        var toggleUsed = new ItemToggledEvent(true, false, null);
        RaiseLocalEvent(ent, ref toggleUsed);
    }

    private void OnMaskUnequipped(Entity<Scp035MaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
        Dirty(ent);

        var toggleUsed = new ItemToggledEvent(true, true, null);
        RaiseLocalEvent(ent, ref toggleUsed);

        RemComp<Scp035MaskUserComponent>(args.Wearer);
    }

    private void OnEquippeAttempt(Entity<Scp035MaskComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!_mobState.IsAlive(ent))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Equipee))
        {
            args.Cancel();

            _stun.TryAddParalyzeDuration(args.Equipee, ent.Comp.EquipAttemptParalyzeDuration);

            if (_net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("scp-035-reject-you"), args.Equipee, args.Equipee, PopupType.LargeCaution);

                var impulse = _random.NextVector2() * ent.Comp.ImpulseModificator;
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
        _container.TryRemoveFromContainer(maskEntity, true);
        _transform.AttachToGridOrMap(maskEntity);

        var deadEnt = Spawn(ent.Comp.DeadSpawnProto, Transform(maskEntity).Coordinates);
        _transform.AttachToGridOrMap(deadEnt);

        QueueDel(ent);
    }

    private void OnMeleeHit(Entity<Scp035MaskUserComponent> ent, ref MeleeHitEvent args)
    {
        args.BonusDamage = args.BaseDamage * ent.Comp.MeleeDamageModificator;
    }

    private void OnStun(Entity<Scp035MaskUserComponent> ent, ref MaskStunActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("scp-035-reject-target"), args.Performer, args.Performer, PopupType.LargeCaution);

            return;
        }

        _stun.TryAddParalyzeDuration(args.Target, ent.Comp.ActionStunDuration);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("scp-035-stun-effect"), args.Target, args.Target, PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnMaskUserStartUp(Entity<Scp035MaskUserComponent> ent, ref ComponentStartup args)
    {
        RaiseLocalEvent(ent, new RejuvenateEvent());

        // Маска овладевает разумом человека и блокирует страх.
        // ЧТО БУДЕТ ЕСЛИ ЧЕЛОВЕК ОВЛАДЕЕТ РАЗУМОМ НА ВСЕ 100????!! - УЖАС!!
        RemCompDeferred<FearComponent>(ent);
    }

    private void OnMaskShutdown(Entity<Scp035MaskUserComponent> ent, ref ComponentShutdown args)
    {
        foreach (var servant in ent.Comp.Servants)
        {
            if (TryComp(servant, out Scp035ServantComponent? servantComp))
                servantComp.User = null;

            _mobState.ChangeMobState(servant, MobState.Dead);
        }

        ToggleActions(ent.Comp, null, ent, false);
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

        foreach (var (order, actionUid) in component.OrderActions)
        {
            _action.SetToggled(actionUid, component.CurrentOrder == order);
            _action.StartUseDelay(actionUid);
        }
    }

    private void ToggleActions(
        Scp035MaskUserComponent maskUserComponent,
        Scp035MaskComponent? maskComponent,
        EntityUid user,
        bool active)
    {
        var existingActions = maskUserComponent.Actions;
        var existingOrderActions = maskUserComponent.OrderActions;

        var newActions = active ? maskComponent?.Actions : null;
        var newOrderActions = active ? maskComponent?.OrderActions : null;

        var orderKeys = newOrderActions != null
            ? newOrderActions.Keys.ToList()
            : existingOrderActions.Keys.ToList();

        var count = Math.Max(
            Math.Max(existingActions.Count, newActions?.Count ?? 0),
            orderKeys.Count
        );

        for (int i = 0; i < count; i++)
        {
            if (i < existingActions.Count)
                _action.RemoveAction(existingActions[i]);

            if (newActions != null && i < newActions.Count)
            {
                var uid = _action.AddAction(user, newActions[i]);
                if (uid != null)
                    existingActions.Add(uid.Value);
            }

            if (i < orderKeys.Count)
            {
                var key = orderKeys[i];

                if (existingOrderActions.TryGetValue(key, out var existingUid))
                    _action.RemoveAction(existingUid);

                if (newOrderActions != null && newOrderActions.TryGetValue(key, out var proto))
                {
                    var uid = _action.AddAction(user, proto);
                    if (uid != null)
                        existingOrderActions[key] = uid.Value;
                }
            }
        }

        if (!active)
        {
            existingActions.Clear();
            existingOrderActions.Clear();
        }
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
