using System.Diagnostics;
using System.Numerics;
using Content.Shared._Scp.Blood;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Blood;

public sealed partial class BloodSplatterSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    private void InitializeParticles()
    {
        SubscribeLocalEvent<BloodParticleComponent, ComponentInit>(OnAdd);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Проходимся по всем частичкам крови и двигаем их
        var query = EntityQueryEnumerator<BloodParticleComponent, FixturesComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var particle, out var fixtures, out var physics))
        {
            if (!particle.Velocity.HasValue)
                continue;

            if (_timing.CurTime < particle.NextMoveTime)
                continue;

            _physics.ApplyLinearImpulse(uid, particle.Velocity.Value, fixtures, physics);
            particle.NextMoveTime = _timing.CurTime + particle.MoveCooldown;
        }
    }

    private void OnAdd(Entity<BloodParticleComponent> ent, ref ComponentInit args)
    {
        Debug.Assert(ent.Comp.MoveTimes == 0);

        ent.Comp.FlyTime += ent.Comp.FlyTime * _random.NextFloat(0f, ent.Comp.FlyTimeVariation);
        Dirty(ent);
    }

    private void SpawnBloodParticles(Entity<BloodSplattererComponent> ent, EntityUid target, float baseAngle, float spreadRadians)
    {
        var count = _random.Next(ent.Comp.Amount.X, ent.Comp.Amount.Y);
        if (count <= 0)
            return;

        _audio.PlayPvs(ent.Comp.ParticleSpawnedSound, target);

        var coords = _transform.GetMoverCoordinates(target);

        for (var i = 0; i <= count; i++)
        {
            var proto = _random.Pick(ent.Comp.Particles);
            var particle = Spawn(proto, coords);

            if (!TryComp<BloodParticleComponent>(particle, out var particleComponent))
            {
                Log.Error($"Found blood PARTICLE without {nameof(BloodParticleComponent)}, prototype: {proto}");
                continue;
            }

            // Если кровь закончилась
            if (!TryTakeBlood(target, ent.Comp.Amount, particle))
            {
                QueueDel(particle);
                return;
            }

            CalculateMove((particle, particleComponent), ent.Comp.Distance, baseAngle, spreadRadians);

            // Спавним лужицу на месте, где будет капля, когда время полета закончится.
            Timer.Spawn(particleComponent.FlyTime, () => SpawnBloodEntity((particle, particleComponent)), _token.Token);
        }
    }

    /// <summary>
    /// Спавнит лужицу крови на месте, куда упала капля по истечению времени полета.
    /// </summary>
    private void SpawnBloodEntity(Entity<BloodParticleComponent> ent)
    {
        var proto = _random.Pick(ent.Comp.BloodEntities);
        var uid = Spawn(proto, Transform(ent).Coordinates);

        if (!_solution.TryGetSolution(uid, SolutionName, out var solutionEntity, out _))
        {
            Log.Error($"Found blood PUDDLE without any solution, prototype: {proto}");
            return;
        }

        if (!_solution.TryGetSolution(ent.Owner, SolutionName, out _, out var particleSolution))
        {
            Log.Error($"Found blood PARTICLE without any solution, prototype: {proto}");
            return;
        }

        _audio.PlayPvs(ent.Comp.LandSound, uid);

        _solution.TryAddSolution(solutionEntity.Value, particleSolution);
        QueueDel(ent);
    }

    /// <summary>
    /// Рассчитывает параметры для перемещения частички крови на основе времени полета и количества промежутков перемещения.
    /// </summary>
    private void CalculateMove(Entity<BloodParticleComponent> ent, Vector2 distance, float baseAngle, float spreadRadians)
    {
        // Задаем количество раз, когда частику крови будет толкать физика.
        // Это количество зависит от времени полета и указанного числа промежутков.
        ent.Comp.MoveCooldown = ent.Comp.FlyTime / ent.Comp.MoveTimes;

        // Рассчитываем нужную скорость движения исходя из расстояния, которое нужно пройти,
        // И количества промежутков, которые у нас будут.
        ent.Comp.Speed = distance / ent.Comp.MoveTimes;

        // Вычисляем вектор движения частички
        var randomOffset = _random.NextFloat(-spreadRadians / 2f, spreadRadians / 2f);
        var angle = baseAngle + randomOffset;

        if (ent.Comp.Speed == Vector2.Zero)
            Log.Error($"Found blood PARTICLE with zero speed {ToPrettyString(ent)}");

        var speed = _random.NextFloat(ent.Comp.Speed.X, ent.Comp.Speed.Y);
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

        ent.Comp.Velocity = direction * speed;
    }
}
