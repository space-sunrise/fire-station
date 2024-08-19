using Content.Shared._Scp.Scp096;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System
{

    private void InitTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void UpdateTargets(float frameTime)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var targetUid, out var targetComponent))
        {
            targetComponent._timeSinceLastHit += frameTime;

            if (targetComponent._timeSinceLastHit < 4f || TryComp<Scp096StunnedComponent>(targetUid, out var _))
            {
                continue;
            }

            targetComponent._timeSinceLastHit = 0f;
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
        if (!_mobStateSystem.IsIncapacitated(ent.Owner))
        {
            return;
        }

        RemComp<Scp096TargetComponent>(ent.Owner);
    }
}
