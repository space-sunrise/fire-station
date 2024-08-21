using Content.Shared._Scp.Scp096;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System
{

    private void InitTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentShutdown>(OnTargetShutdown);
        SubscribeLocalEvent<Scp096TargetComponent, DamageChangedEvent>(OnHit);
    }

    private void OnHit(Entity<Scp096TargetComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<Scp096Component>(args.Origin, out var scpComponent))
        {
            return;
        }

        ent.Comp.TimesHitted++;

        if (ent.Comp.TimesHitted >= 2)
        {
            _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(ent.Owner,
                SleepStatusEffectKey,
                TimeSpan.FromSeconds(ent.Comp.SleepTime),
                false);

            RemComp<Scp096TargetComponent>(ent);
        }
    }

    private void UpdateTargets(float frameTime)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var targetUid, out var targetComponent))
        {
            targetComponent.HitTimeAcc += frameTime;
            _sawmill.Log(LogLevel.Error, targetComponent.HitTimeAcc.ToString());
            if (targetComponent.HitTimeAcc > targetComponent.HitWindow)
            {
                targetComponent.HitTimeAcc = 0f;
                targetComponent.TimesHitted = 0;
            }
        }
    }

    private void OnTargetShutdown(Entity<Scp096TargetComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var scpEntityUid, out var scp096Component))
        {
            RemoveTarget(new Entity<Scp096Component>(scpEntityUid, scp096Component), ent.Owner, false);
        }
    }

    private void OnTargetStateChanged(Entity<Scp096TargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            return;
        }

        RemComp<Scp096TargetComponent>(ent.Owner);
    }
}
