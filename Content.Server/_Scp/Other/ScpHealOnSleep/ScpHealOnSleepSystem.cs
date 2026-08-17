using Content.Server.Actions;
using Content.Shared._Scp.Other.ScpSleep;
using Content.Shared._Scp.Other.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server._Scp.Other.ScpSleep;

public sealed class ScpHealOnSleepSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpHealOnSleepComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScpHealOnSleepComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScpHealOnSleepComponent, ScpSleepActionEvent>(OnSleepAction);
        SubscribeLocalEvent<ScpHealOnSleepComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ScpHealOnSleepComponent, SleepStateChangedEvent>(OnSleepChanged);
    }

    private void OnMapInit(Entity<ScpHealOnSleepComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.AddAction)
            return;

        var actionEnt = _actions.AddAction(ent, ent.Comp.ActionProto);
        ent.Comp.ActionEnt = actionEnt;
    }

    private void OnShutdown(Entity<ScpHealOnSleepComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
        ent.Comp.ActionEnt = null;
    }

    private void OnSleepAction(Entity<ScpHealOnSleepComponent> ent, ref ScpSleepActionEvent args)
    {
        args.Handled = TrySleep(ent, ent.Comp.HibernationDuration);
    }

    private void OnMobStateChanged(Entity<ScpHealOnSleepComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.HibernationStates == null)
            return;

        if (!ent.Comp.HibernationStates.Contains(args.NewMobState))
            return;

        if (!TrySleep(ent, ent.Comp.HibernationDurationOnHibernationState))
            return;

        if (ent.Comp.CritSound == null)
            return;

        _audio.PlayPvs(ent.Comp.CritSound, ent);
    }

    private void OnSleepChanged(Entity<ScpHealOnSleepComponent> ent, ref SleepStateChangedEvent args)
    {
        if (TryComp<BloodstreamComponent>(ent, out var bloodstreamComponent))
        {
            if (args.FellAsleep)
                bloodstreamComponent.BloodRefreshAmount = ent.Comp.FellAsleepBloodRefreshAmount;
            else
                bloodstreamComponent.BloodRefreshAmount = ent.Comp.BaseBloodRefreshAmount;

            DirtyField(ent, bloodstreamComponent, nameof(BloodstreamComponent.BloodRefreshAmount));
        }

        _appearance.SetData(ent, ScpHealOnSleepVisuals.Sleeping, args.FellAsleep);
    }

    public bool TrySleep(Entity<ScpHealOnSleepComponent> ent, TimeSpan hibernationDuration)
    {
        if (HasComp<SleepingComponent>(ent))
            return false;

        if (!_sleeping.TrySleeping(ent.Owner))
            return false;

        if (!_statusEffects.TryAddStatusEffectDuration(ent, ent.Comp.StatusEffect, hibernationDuration))
            return false;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var querySleeping = EntityQueryEnumerator<ScpHealOnSleepComponent, SleepingComponent>();
        while (querySleeping.MoveNext(out var uid, out var scpSleepComponent, out _))
        {
            if (scpSleepComponent.HibernationHealingRate == null)
                continue;

            _damageable.TryChangeDamage(uid, scpSleepComponent.HibernationHealingRate * frameTime);
        }
    }
}
