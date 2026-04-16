using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Content.Server.Spawners.Components;

namespace Content.Server._Scp.Other.LimitedTimedSpawn;

public sealed partial class LimitedTimedSpawnSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<string, List<EntityUid>> _spawnedEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedTimedSpawnComponent, MapInitEvent>(OnStartup);
    }

    private void OnStartup(Entity<LimitedTimedSpawnComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.IntervalSeconds;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var list in _spawnedEntities.Values)
            list.RemoveAll(uid => !EntityManager.EntityExists(uid));

        var query = EntityQueryEnumerator<LimitedTimedSpawnComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextSpawn)
                continue;

            component.NextSpawn = _timing.CurTime + component.IntervalSeconds;

            if (!_spawnedEntities.TryGetValue(component.Prototype, out var spawnedList))
            {
                spawnedList = new List<EntityUid>();
                _spawnedEntities[component.Prototype] = spawnedList;
            }

            if (spawnedList.Count >= component.EntitiesLimit)
                continue;

            if (!_random.Prob(component.Chance))
                continue;

            var newEnt = Spawn(component.Prototype, Transform(uid).Coordinates);
            spawnedList.Add(newEnt);

            if (!component.CopyCopies)
                RemCompDeferred<TimedSpawnerComponent>(newEnt);

            if (component.ImpulseStrength != 0)
                ThrowRand(newEnt, component.ImpulseStrength);
        }
    }

    private void ThrowRand(EntityUid uid, float impulseStrength)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) ||
            impulseStrength <= 0)
            return;

        var angle = _random.NextAngle();
        var direction = angle.ToVec();

        _physics.ApplyLinearImpulse(uid, direction * impulseStrength, body: physics);
        _physics.ApplyAngularImpulse(uid, _random.NextFloat(-0.5f, 0.5f), body: physics);
    }
}
