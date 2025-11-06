using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._Scp.Other.NoSmallMobsCollision;

/// <summary>
/// Система для удаления коллизии у мелких мобов, так как она им не нужна.
/// </summary>
public sealed class NoSmallMobsCollisionSystem : EntitySystem
{
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobCollisionComponent, MapInitEvent>(OnMapInit);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }


    private void OnMapInit(Entity<MobCollisionComponent> ent, ref MapInitEvent args)
    {
        if (!_physicsQuery.TryComp(ent, out var physics))
            return;

        // мелкие мобы не должны сталкиваться, в идеале сделать столкновение мелкие-мелкие и большие-большие,
        // но ради оптимизации можно просто вырубить ее у мелких, все равно всем похуй будет
        if ((physics.CollisionMask & (int)CollisionGroup.SmallMobMask) != 0)
            RemComp<MobCollisionComponent>(ent);
    }
}
