using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Scp.Scp012;

public abstract class SharedScp012System : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp012Component, GettingDroppedAttemptEvent>(OnGettingDropped);
    }

    private void OnGettingDropped(Entity<Scp012Component> ent, ref GettingDroppedAttemptEvent args)
    {
        if (!TryComp<Scp012VictimComponent>(args.User, out var victim))
            return;

        if (victim.LifeStage <= ComponentLifeStage.Initialized)
            return;

        if (victim.LifeStage >= ComponentLifeStage.Stopping)
            return;

        args.Cancelled = true;
    }
}
