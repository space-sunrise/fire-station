using Content.Server.Actions;
using Content.Shared._Scp.Other.ScpSleep;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server._Scp.Other.ScpSleep;

public sealed class ScpSleepSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpSleepComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ScpSleepComponent, ScpSleepActionEvent>(OnSleepAction);
        SubscribeLocalEvent<ScpSleepComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ScpSleepComponent, SleepStateChangedEvent>(OnSleepChanged);
    }
    public void OnInit(Entity<ScpSleepComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent, ent.Comp.ActionProto);
    }

    private void OnSleepAction(Entity<ScpSleepComponent> ent, ref ScpSleepActionEvent args)
    {
        args.Handled = TrySleep(ent);
    }

    private void OnMobStateChanged(Entity<ScpSleepComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.HibernationOnCriticalState)
            return;

        if (args.NewMobState != MobState.Critical)
            return;

        TrySleep(ent, ent.Comp.HibernationDurationOnCriticalState);

        if (ent.Comp.CritSound == null)
            return;

        _audio.PlayPvs(ent.Comp.CritSound, ent);
    }

    private void OnSleepChanged(Entity<ScpSleepComponent> ent, ref SleepStateChangedEvent args)
    {
        if (TryComp<BloodstreamComponent>(ent, out var bloodstreamComponent))
        {
            if (args.FellAsleep)
                bloodstreamComponent.BloodRefreshAmount = 20;
            else
                bloodstreamComponent.BloodRefreshAmount = 1;

            Dirty(ent, bloodstreamComponent);
        }

        _appearanceSystem.SetData(ent, ScpSleepVisuals.Sleeping, args.FellAsleep);
    }

    public bool TrySleep(Entity<ScpSleepComponent> ent, float hibernationDuration = 0)
    {
        if (HasComp<SleepingComponent>(ent))
            return false;

        if (!_sleepingSystem.TrySleeping(ent.Owner))
            return false;

        hibernationDuration = hibernationDuration == 0 ? ent.Comp.HibernationDuration : hibernationDuration;
        _statusEffects.TryAddStatusEffectDuration(ent, ent.Comp.StatusEffect, TimeSpan.FromSeconds(hibernationDuration));

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var querySleeping = EntityQueryEnumerator<ScpSleepComponent, SleepingComponent>();
        while (querySleeping.MoveNext(out var uid, out var scpSleepComponent, out _))
        {
            if (!scpSleepComponent.HibernationHealing)
                continue;

            _damageableSystem.TryChangeDamage(uid, scpSleepComponent.HibernationHealingRate * frameTime);
        }
    }
}
