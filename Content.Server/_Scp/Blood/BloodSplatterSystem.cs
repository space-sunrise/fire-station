using System.Numerics;
using System.Threading;
using Content.Shared._Scp.Blood;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Blood;

/// <summary>
/// Система, управляющая брызгами крови.
/// Позволяет создавать красивые партиклы крови, которые некоторое время разлетаются в разные стороны.
/// </summary>
public sealed class BloodSplatterSystem : EntitySystem
{
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
        SubscribeLocalEvent<BloodSplatterComponent, ComponentStartup>(OnSplatterStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
    }

    private void OnAttacked(Entity<BloodstreamComponent> ent, ref AttackedEvent args)
    {
        if (!TryComp<BloodSplattererComponent>(args.User, out var splatterer))
            return;

        TrySplat((args.User, splatterer), ent);
    }

    /// <summary>
    /// Пытается создать брызги крови, летящие от цели при ее атаке.
    /// </summary>
    /// <remarks>
    /// Количество брызг, их скорость и т.п. определяется параметрами в компоненте.
    /// </remarks>
    /// <param name="ent">Сущность, которая атакует персонажа и поддерживает брызги при ударе</param>
    /// <param name="target">Цель, которую атакуют</param>
    public bool TrySplat(Entity<BloodSplattererComponent> ent, EntityUid target)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return false;

        // Броня не пробита
        if (TryComp<PierceableComponent>(target, out var pierceable) && pierceable.Level > ent.Comp.PierceLevel)
            return false;

        if (!_solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var victimBloodSolution)
            || victimBloodSolution.Volume == FixedPoint2.Zero)
            return false;

        var amountToTake = FixedPoint2.Min(ent.Comp.BloodToTake, victimBloodSolution.Volume);
        var split = _solution.SplitSolution(bloodstream.BloodSolution.Value, amountToTake);
        var victimPosition = _transform.GetWorldPosition(target);
        var victimCoords = _transform.GetMoverCoordinates(target);
        var attackerPosition = _transform.GetWorldPosition(ent);
        var count = _random.Next(ent.Comp.Amount.X, ent.Comp.Amount.Y);

        if (count == 0)
            return false;

        _audio.PlayPvs(ent.Comp.Sound, target);

        // Вычисляем базовое направление от атакующего к жертве
        var baseDirection = (victimPosition - attackerPosition).Normalized();
        var baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        for (var i = 0; i < count; i++)
        {
            var proto = _random.Pick(ent.Comp.Particles);
            var particle = Spawn(proto, victimCoords);

            if (!TryComp<BloodSplatterComponent>(particle, out var splatter))
                continue;

            if (!TryComp<PhysicsComponent>(particle, out var physics))
                continue;

            if (!_solution.TryGetSolution(particle, splatter.SolutionName, out var solutionEntity, out _))
                continue;

            _solution.TryAddSolution(solutionEntity.Value, split.SplitSolution(split.Volume / (count - i)));

            // Вычисляем случайный угол в пределах заданного разброса
            var spreadRadians = MathF.PI * ent.Comp.SpreadAngle / 180f; // Конвертируем градусы в радианы
            var randomOffset = _random.NextFloat(-spreadRadians / 2f, spreadRadians / 2f);
            var angle = baseAngle + randomOffset;

            var speed = _random.NextFloat(ent.Comp.Speed.X, ent.Comp.Speed.Y);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            _physics.SetLinearVelocity(particle, direction * speed, body: physics);
            _transform.SetWorldRotation(particle, angle + NormalizedRotationAngle);

            Timer.Spawn(splatter.LifeTime, () => CleanupEntity(particle), _token.Token);
        }

        return true;
    }

    private void OnSplatterStartup(Entity<BloodSplatterComponent> ent, ref ComponentStartup args)
    {
        // Вероятность проигрывания звука.
        // Предотвращает переполненность буфера звуковой библиотеки, когда множество брызгов создаются
        if (!_random.Prob(ent.Comp.SoundProbability))
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent);
    }

    /// <summary>
    /// Убирает лишние компоненты, после того как сущность точно прекратила движение.
    /// </summary>
    private void CleanupEntity(EntityUid uid)
    {
        // TODO: Исправить спам варнингами на клиенте
        RemCompDeferred<PhysicsComponent>(uid);
    }

    private void OnRestart(RoundRestartCleanupEvent _)
    {
        _token.Cancel();
        _token.Dispose();
        _token = new();
    }
}

