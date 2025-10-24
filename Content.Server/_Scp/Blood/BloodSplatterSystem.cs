using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using Content.Shared._Scp.Blood;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Blood;

/// <summary>
/// Система, управляющая брызгами крови.
/// Позволяет создавать красивые партиклы крови, которые некоторое время разлетаются в разные стороны.
/// </summary>
public sealed class BloodSplatterSystem : SharedBloodSplatterSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    private CancellationTokenSource _token = new();

    private static readonly Angle NormalizedRotationAngle = Angle.FromDegrees(-90);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BloodParticleComponent, ComponentInit>(OnStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        Log.Level = LogLevel.Info;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

    private void OnAttacked(Entity<BloodstreamComponent> ent, ref AttackedEvent args)
    {
        if (!TryGetSource(args.User, args.Used, out var source))
            return;

        TrySplat(source.Value, ent);
    }

    /// <summary>
    /// Пытается создать брызги крови, летящие от цели при ее атаке.
    /// </summary>
    /// <remarks>
    /// Количество брызг, их скорость и т.п. определяется параметрами в компоненте.
    /// </remarks>
    /// <param name="ent">Сущность, которая атакует персонажа и поддерживает брызги при ударе</param>
    /// <param name="target">Цель, которую атакуют</param>
    public bool TrySplat(Entity<BloodSplattererComponent> ent, Entity<BloodstreamComponent> target)
    {
        var count = _random.Next(ent.Comp.Amount.X, ent.Comp.Amount.Y);
        if (count == 0)
            return false;

        // Броня не пробита
        if (TryComp<PierceableComponent>(target, out var pierceable) && pierceable.Level > ent.Comp.PierceLevel)
            return false;

        if (!_solution.ResolveSolution(target.Owner, target.Comp.BloodSolutionName, ref target.Comp.BloodSolution, out var victimBloodSolution)
            || victimBloodSolution.Volume == FixedPoint2.Zero)
            return false;

        var amountToTake = FixedPoint2.Min(ent.Comp.BloodToTake, victimBloodSolution.Volume);
        var split = _solution.SplitSolution(target.Comp.BloodSolution.Value, amountToTake);

        var victimPosition = _transform.GetWorldPosition(target);
        var victimCoords = _transform.GetMoverCoordinates(target);
        var attackerPosition = _transform.GetWorldPosition(ent);

        _audio.PlayPvs(ent.Comp.Sound, target);

        // Вычисляем базовое направление от атакующего к жертве
        var baseDirection = (victimPosition - attackerPosition).Normalized();
        var baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        // Вычисляем случайный угол в пределах заданного разброса
        var spreadRadians = MathF.PI * ent.Comp.SpreadAngle / 180f; // Конвертируем градусы в радианы

        for (var i = 0; i < count; i++)
        {
            var proto = _random.Pick(ent.Comp.Particles);
            var particle = Spawn(proto, victimCoords);
            var solution = split.SplitSolution(split.Volume / (count - i));

            if (!TryComp<BloodParticleComponent>(particle, out var particleComponent))
            {
                Log.Error($"Found blood particle without {nameof(BloodParticleComponent)}, prototype: {proto}");
                continue;
            }

            // Спавним лужицу на месте, где будет капля, когда время полета закончится.
            Timer.Spawn(particleComponent.FlyTime, () => SpawnBloodEntity((particle, particleComponent), solution), _token.Token);

            var randomOffset = _random.NextFloat(-spreadRadians / 2f, spreadRadians / 2f);
            var angle = baseAngle + randomOffset;

            Debug.Assert(particleComponent.Speed == Vector2.Zero);

            var speed = _random.NextFloat(particleComponent.Speed.X, particleComponent.Speed.Y);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            particleComponent.Velocity = direction * speed;
            Log.Info($"{speed} | {particleComponent.Velocity}");
            //_transform.SetWorldRotation(particle, angle + NormalizedRotationAngle);
        }

        return true;
    }

    private void OnStartup(Entity<BloodParticleComponent> ent, ref ComponentInit args)
    {
        // Задаем количество раз, когда частику крови будет толкать физика.
        // Это количество зависит от времени полета и указанного числа промежутков.
        ent.Comp.MoveCooldown = ent.Comp.FlyTime / ent.Comp.MoveTimes;

        // Рассчитываем нужную скорость движения исходя из расстояния, которое нужно пройти,
        // И количества промежутков, которые у нас будут.
        ent.Comp.Speed = ent.Comp.Distance / ent.Comp.MoveTimes;

        // Вероятность проигрывания звука.
        // Предотвращает переполненность буфера звуковой библиотеки, когда множество брызгов создаются
        if (!_random.Prob(ent.Comp.SoundProbability))
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent);
    }

    /// <summary>
    /// Спавнит лужицу крови на месте, куда упала капля по истечению времени полета.
    /// </summary>
    private void SpawnBloodEntity(Entity<BloodParticleComponent> ent, Solution solution)
    {
        var proto = _random.Pick(ent.Comp.BloodEntities);
        var uid = Spawn(proto, Transform(ent).Coordinates);

        if (!_solution.TryGetSolution(uid, ent.Comp.SolutionName, out var solutionEntity, out _))
        {
            Log.Error($"Found blood puddle without any solution, prototype: {proto}");
            return;
        }

        _solution.TryAddSolution(solutionEntity.Value, solution);

        QueueDel(ent);
    }

    private bool TryGetSource(Entity<BloodSplattererComponent?> user,
        Entity<BloodSplattererComponent?> used,
        [NotNullWhen(true)] out Entity<BloodSplattererComponent>? splatterer)
    {
        splatterer = null;

        if (Resolve(user, ref user.Comp, false))
        {
            splatterer = (user, user.Comp);
            return true;
        }

        if (Resolve(used, ref used.Comp, false))
        {
            splatterer = (used, used.Comp);
            return true;
        }

        return false;
    }

    private void OnRestart(RoundRestartCleanupEvent _)
    {
        _token.Cancel();
        _token.Dispose();
        _token = new();
    }
}

