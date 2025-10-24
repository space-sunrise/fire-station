using System.Diagnostics;
using System.Numerics;
using Content.Shared._Scp.Blood;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
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
        SubscribeLocalEvent<BloodParticleComponent, ComponentInit>(OnInit);
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

    private void OnInit(Entity<BloodParticleComponent> ent, ref ComponentInit args)
    {
        // Вероятность проигрывания звука.
        // Предотвращает переполненность буфера звуковой библиотеки, когда множество брызгов создаются
        if (!_random.Prob(ent.Comp.SoundProbability))
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent);
    }

    private void SpawnBloodParticles(Entity<BloodSplattererComponent> ent,
        EntityCoordinates victimCoords,
        ref Entity<SolutionComponent>? bloodSolutionEntity,
        Solution bloodSolution,
        float baseAngle,
        float spreadRadians)
    {
        if (!bloodSolutionEntity.HasValue)
        {
            Log.Error("Found blood PARTICLE with null blood Solution Entity");
            return;
        }

        var count = _random.Next(ent.Comp.Amount.X, ent.Comp.Amount.Y);

        for (var i = 0; i <= count; i++)
        {
            var randomizedBlood =
                _random.NextFloat(ent.Comp.BloodToTakePerParticle.X, ent.Comp.BloodToTakePerParticle.Y);

            var amountToTake = FixedPoint2.Min(randomizedBlood, bloodSolution.Volume);
            var solution = _solution.SplitSolution(bloodSolutionEntity.Value, amountToTake);

            // Если в человеке закончилась кровь - больше не спавним.
            if (solution.Volume == FixedPoint2.Zero)
                continue;

            var proto = _random.Pick(ent.Comp.Particles);
            var particle = Spawn(proto, victimCoords);

            if (!TryComp<BloodParticleComponent>(particle, out var particleComponent))
            {
                Log.Error($"Found blood PARTICLE without {nameof(BloodParticleComponent)}, prototype: {proto}");
                continue;
            }

            // Временно передает solution сюда, чтобы частичка крови окрасилась в нужный цвет
            if (!_solution.TryGetSolution(particle, SolutionName, out var solutionEntity, out _))
            {
                Log.Error($"Found blood PARTICLE without any solution, prototype: {proto}");
                return;
            }
            _solution.TryAddSolution(solutionEntity.Value, solution);

            CalculateMove((particle, particleComponent), ent.Comp.Distance, baseAngle, spreadRadians);

            // Спавним лужицу на месте, где будет капля, когда время полета закончится.
            Timer.Spawn(particleComponent.FlyTime, () => SpawnBloodEntity((particle, particleComponent), solution), _token.Token);
        }
    }

    /// <summary>
    /// Спавнит лужицу крови на месте, куда упала капля по истечению времени полета.
    /// </summary>
    private void SpawnBloodEntity(Entity<BloodParticleComponent> ent, Solution solution)
    {
        var proto = _random.Pick(ent.Comp.BloodEntities);
        var uid = Spawn(proto, Transform(ent).Coordinates);

        if (!_solution.TryGetSolution(uid, SolutionName, out var solutionEntity, out _))
        {
            Log.Error($"Found blood puddle without any solution, prototype: {proto}");
            return;
        }

        _solution.TryAddSolution(solutionEntity.Value, solution);

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

        Debug.Assert(ent.Comp.Speed == Vector2.Zero);

        var speed = _random.NextFloat(ent.Comp.Speed.X, ent.Comp.Speed.Y);
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

        ent.Comp.Velocity = direction * speed;
        Log.Info($"{speed} | {ent.Comp.Velocity}");
    }
}
