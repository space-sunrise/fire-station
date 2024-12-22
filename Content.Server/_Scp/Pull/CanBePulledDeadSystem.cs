using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Scp.Pull;

public sealed class CanBePulledDeadSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CanBePulledDeadComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<CanBePulledDeadComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsIncapacitated(ent))
        {
            EnsureComp<PullableComponent>(ent);
        }
        else if (_mobState.IsAlive(ent))
        {
            RemComp<PullableComponent>(ent.Owner);
        }
    }
}
