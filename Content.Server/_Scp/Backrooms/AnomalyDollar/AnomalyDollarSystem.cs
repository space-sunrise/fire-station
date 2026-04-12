using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared._Scp.Backrooms.AnomalyDollar;
using Content.Server.Explosion.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Explosion.Components;

namespace Content.Server._Scp.Backrooms.AnomalyDollar;

public sealed partial class AnomalyDollarSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyDollarComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AnomalyDollarComponent, EntityTerminatingEvent>(OnTerminating);
    }

    private void OnStartup(Entity<AnomalyDollarComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextCloneTime = _timing.CurTime + ent.Comp.CloneDelay; // установка базового времени создания новой копии
        Dirty(ent, ent.Comp);
    }

    private void OnTerminating(Entity<AnomalyDollarComponent> ent, ref EntityTerminatingEvent args)
    {
        if (LifeStage(ent) >= EntityLifeStage.Deleted) // перезапуск раунда
            return;

        if (Count<AnomalyDollarComponent>() <= 1 &&
            TryComp<ExplosiveComponent>(ent, out var explosionComp))
        {
            _explosion.QueueExplosion(
                _transform.GetMapCoordinates(ent),
                explosionComp.ExplosionType,
                totalIntensity: explosionComp.TotalIntensity,
                slope: explosionComp.IntensitySlope,
                maxTileIntensity: explosionComp.MaxIntensity,
                cause: ent.Owner,
                addLog: true
            );
        }
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
        var allQuery = EntityQueryEnumerator<AnomalyDollarComponent>();
        while (allQuery.MoveNext(out _, out _))
        {
            totalCount++;
        }

        var query = EntityQueryEnumerator<AnomalyDollarComponent>();
        var readyToClone = new List<(string Proto, EntityCoordinates Coords, float Strength, AnomalyDollarComponent Comp, EntityUid Uid)>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime >= component.NextCloneTime)
            {
                var proto = Prototype(uid)?.ID;
                if (proto != null)
                {
                    readyToClone.Add((proto, Transform(uid).Coordinates, component.ImpulseStrength, component, uid));
                }
            }
        }

        for (var i = 0; i < readyToClone.Count; i++)
        {
            var (proto, coords, strength, comp, uid) = readyToClone[i];

            if (totalCount < comp.CopiesLimit)
            {
                if (_random.Prob(comp.CloneChance)) // проверка шанса спавна
                {
                    var newEnt = Spawn(proto, coords);
                    ThrowRand(newEnt, strength);

                    totalCount++;
                }
            }

            comp.NextCloneTime = _timing.CurTime + comp.CloneDelay;
            Dirty(uid, comp);
        }
    }
}
