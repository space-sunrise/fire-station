using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Content.Server._Scp.Other.LimitedTimedSpawn;

public sealed partial class LimitedTimedSpawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    private readonly List<(string Proto, EntityCoordinates Coords, float Strength, LimitedTimedSpawnComponent Comp)> _readyToCloneBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedTimedSpawnComponent, MapInitEvent>(OnStartup);
    }

    private void OnStartup(Entity<LimitedTimedSpawnComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.IntervalSeconds;
    }

    private void ThrowRand(EntityUid uid, float impulseStrength)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var angle = _random.NextAngle();
        var direction = angle.ToVec();

        _physics.ApplyLinearImpulse(uid, direction * impulseStrength, body: physics);
        _physics.ApplyAngularImpulse(uid, _random.NextFloat(-0.5f, 0.5f), body: physics);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var totalCount = 0;
        var query = EntityQueryEnumerator<LimitedTimedSpawnComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime >= component.NextSpawn)
            {
                var proto = component.Prototype;
                totalCount++;
                _readyToCloneBuffer.Add((proto, Transform(uid).Coordinates, component.ImpulseStrength, component));
            }
        }

        for (var i = 0; i < _readyToCloneBuffer.Count; i++)
        {
            var (proto, coords, strength, comp) = _readyToCloneBuffer[i];

            if (totalCount < comp.EntitiesLimit)
            {
                if (_random.Prob(comp.Chance)) // проверка шанса спавна
                {
                    var newEnt = Spawn(proto, coords);

                    if (strength != 0)
                        ThrowRand(newEnt, strength);

                    totalCount++;
                }
            }

            comp.NextSpawn = _timing.CurTime + comp.IntervalSeconds;
        }
    }
}
