using Content.Shared.Mobs;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Scp.Pull;

public sealed class CanBePulledDeadSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CanBePulledDeadComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<CanBePulledDeadComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Alive:
                if (HasComp<PullableComponent>(ent))
                    RemComp<PullableComponent>(ent.Owner);
                break;
            case MobState.Critical:
                EnsureComp<PullableComponent>(ent);
                break;
            case MobState.Dead:
                EnsureComp<PullableComponent>(ent);
                break;
        }
    }
}
