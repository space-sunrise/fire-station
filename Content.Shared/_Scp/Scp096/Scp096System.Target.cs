using Content.Shared.Damage;
using Content.Shared.Mobs;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    protected void InitTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentShutdown>(OnTargetShutdown);
        SubscribeLocalEvent<Scp096TargetComponent, DamageChangedEvent>(OnHit);
    }

    private void OnHit(Entity<Scp096TargetComponent> ent, ref DamageChangedEvent args)
    {
        if (!HasComp<Scp096Component>(args.Origin))
            return;

        ent.Comp.TimesHitted++;

        if (ent.Comp.TimesHitted < 4)
            return;

        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, TimeSpan.FromSeconds(ent.Comp.SleepTime));
        RemComp<Scp096TargetComponent>(ent);
    }

    private void UpdateTargets(float frameTime)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out _, out var targetComponent))
        {
            targetComponent.HitTimeAcc += frameTime;

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
        if (!_mobStateSystem.IsDead(args.Target))
            return;

        RemComp<Scp096TargetComponent>(ent.Owner);
    }
}
