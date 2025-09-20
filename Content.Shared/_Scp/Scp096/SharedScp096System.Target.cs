using Content.Shared.Mobs;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    private void InitTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void OnTargetShutdown(Entity<Scp096TargetComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var scpEntityUid, out var scp096Component))
        {
            RemoveTarget((scpEntityUid, scp096Component), ent.AsNullable(), false);
        }
    }

    private void OnTargetStateChanged(Entity<Scp096TargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_mobState.IsDead(args.Target))
            return;

        RemComp<Scp096TargetComponent>(ent);
    }
}
