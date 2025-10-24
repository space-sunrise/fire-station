using System.Diagnostics.CodeAnalysis;
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
using Robust.Shared.Random;

namespace Content.Server._Scp.Blood;

/// <summary>
/// Система, управляющая брызгами крови.
/// Позволяет создавать красивые частички крови, которые некоторое время разлетаются в разные стороны.
/// </summary>
public sealed partial class BloodSplatterSystem : SharedBloodSplatterSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const string SolutionName = "blood";

    private CancellationTokenSource _token = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        InitializeParticles();

        Log.Level = LogLevel.Info;
    }

    private void OnAttacked(Entity<BloodstreamComponent> ent, ref AttackedEvent args)
    {
        if (!TryGetSource(args.User, args.Used, out var source))
            return;

        if (!_random.Prob(source.Value.Comp.Probability))
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
        // Броня не пробита
        if (TryComp<PierceableComponent>(target, out var pierceable) && pierceable.Level > ent.Comp.PierceLevel)
            return false;

        if (!_solution.ResolveSolution(target.Owner, target.Comp.BloodSolutionName, ref target.Comp.BloodSolution, out var bloodSolution)
            || bloodSolution.Volume == FixedPoint2.Zero)
            return false;


        Splat(ent, target, ref target.Comp.BloodSolution, bloodSolution);

        return true;
    }

    /// <summary>
    /// Создает капли крови, летящие от цели.
    /// </summary>
    /// <param name="ent">Сущность, которая инициировала выделение частичек крови(оружие, персонаж)</param>
    /// <param name="target">Цель, которая будет терять кровь и испускать частички</param>
    /// <param name="bloodSolutionEntity">Сущность с реагентом крови цели</param>
    /// <param name="bloodSolution">Реагенты крови цели</param>
    private void Splat(Entity<BloodSplattererComponent> ent,
        Entity<BloodstreamComponent> target,
        ref Entity<SolutionComponent>? bloodSolutionEntity,
        Solution bloodSolution)
    {
        var victimPosition = _transform.GetWorldPosition(target);
        var victimCoords = _transform.GetMoverCoordinates(target);
        var attackerPosition = _transform.GetWorldPosition(ent);

        _audio.PlayPvs(ent.Comp.Sound, target);

        // Вычисляем базовое направление от атакующего к жертве
        var baseDirection = (victimPosition - attackerPosition).Normalized();
        var baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        // Вычисляем случайный угол в пределах заданного разброса
        var spreadRadians = MathF.PI * ent.Comp.SpreadAngle / 180f; // Конвертируем градусы в радианы

        CreateBloodLine(ent, target, ref bloodSolutionEntity, bloodSolution);
        SpawnBloodParticles(ent, victimCoords, ref bloodSolutionEntity, bloodSolution, baseAngle, spreadRadians);
    }

    /// <summary>
    /// Пытается определить инициатора, который запустит выделение частичек крови.
    /// Это может быть атакующий персонаж или оружие, которым атакуют.
    /// </summary>
    /// <param name="user">Персонаж, который атакует</param>
    /// <param name="used">Предмет, который используется для атаки</param>
    /// <param name="splatterer">Инициатор выделения частичек крови</param>
    /// <returns>Удалось ли найти инициатора</returns>
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

