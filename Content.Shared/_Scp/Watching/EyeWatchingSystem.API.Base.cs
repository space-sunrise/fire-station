using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Storage.Components;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<InsideEntityStorageComponent> _insideStorageQuery;
    private EntityQuery<BlinkableComponent> _blinkableQuery;

    private void InitializeApi()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _insideStorageQuery = GetEntityQuery<InsideEntityStorageComponent>();
        _blinkableQuery = GetEntityQuery<BlinkableComponent>();
    }

    public bool TryGetAllEntitiesVisibleTo<T>(
        Entity<TransformComponent?> ent,
        List<Entity<T>> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
        where T : IComponent
    {
        using var searchSet = HashSetPoolEntity<T>.Rent();
        return TryGetAllEntitiesVisibleTo(ent, potentialWatchers, searchSet.Value, type, flags);
    }

    /// <summary>
    /// Получает и возвращает всех потенциально смотрящих на указанную цель.
    /// </summary>
    /// <remarks>
    /// В методе нет проверок на дополнительные состояния, такие как моргание/закрыты ли глаза/поле зрения т.п.
    /// Единственная проверка - можно ли физически увидеть цель(т.е. не закрыта ли она стеной и т.п.)
    /// </remarks>
    /// <param name="ent">Цель, для которой ищем потенциальных смотрящих</param>
    /// <param name="potentialWatchers">Список всех, кто потенциально видит цель</param>
    /// <param name="type">Требуемая прозрачность линии видимости.</param>
    /// <param name="targets">Заранее заготовленный список, который будет использоваться в <see cref="EntityLookupSystem"/></param>
    /// <param name="flags">Список флагов для поиска целей в <see cref="EntityLookupSystem"/></param>
    /// <returns>Удалось ли найти хоть кого-то</returns>
    private bool TryGetAllEntitiesVisibleTo<T>(
        Entity<TransformComponent?> ent,
        List<Entity<T>> potentialWatchers,
        HashSet<Entity<T>> searchSet,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
        where T : IComponent
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        searchSet.Clear();
        _lookup.GetEntitiesInRange(ent.Comp.Coordinates, SeeRange, searchSet, flags);

        foreach (var target in searchSet)
        {
            if (!IsInProximity(ent, target, type))
                continue;

            potentialWatchers.Add(target);
        }

        return potentialWatchers.Count != 0;
    }

    public bool TryGetAnyEntitiesVisibleTo<T>(
        Entity<TransformComponent?> viewer,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
        where T : IComponent
    {
        using var searchSet = HashSetPoolEntity<T>.Rent();
        if (!TryGetAnyEntitiesVisibleTo(viewer, out _, searchSet.Value, type, flags))
            return false;

        return true;
    }

    public bool TryGetAnyEntitiesVisibleTo<T>(
        Entity<TransformComponent?> viewer,
        [NotNullWhen(true)] out Entity<T>? firstVisible,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
        where T : IComponent
    {
        firstVisible = null;

        using var searchSet = HashSetPoolEntity<T>.Rent();
        if (!TryGetAnyEntitiesVisibleTo(viewer, out var first, searchSet.Value, type, flags))
            return false;

        firstVisible = first;
        return true;
    }

    private bool TryGetAnyEntitiesVisibleTo<T>(
        Entity<TransformComponent?> viewer,
        [NotNullWhen(true)] out Entity<T>? firstVisible,
        HashSet<Entity<T>> searchSet,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
        where T : IComponent
    {
        firstVisible = null;

        if (!Resolve(viewer.Owner, ref viewer.Comp))
            return false;

        searchSet.Clear();
        _lookup.GetEntitiesInRange(viewer.Comp.Coordinates, SeeRange, searchSet, flags);

        foreach (var target in searchSet)
        {
            if (!IsInProximity(viewer, target, type))
                continue;

            firstVisible = target;
            return true;
        }

        return false;
    }

    private bool IsInProximity(EntityUid ent, EntityUid target, LineOfSightBlockerLevel type)
    {
        if (target == ent)
            return false;

        if (!_proximity.IsRightType(ent, target, type, out _))
            return false;

        return true;
    }

    /// <summary>
    /// Проверка на то, может ли смотрящий видеть цель
    /// </summary>
    /// <param name="viewer">Смотрящий</param>
    /// <param name="target">Цель, которую проверяем</param>
    /// <param name="useFov">Применять ли проверку на поле зрения?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол поля зрения</param>
    /// <returns>Видит ли смотрящий цель</returns>
    public bool CanSee(Entity<BlinkableComponent?> viewer, EntityUid target, bool useFov = true, float? fovOverride = null)
    {
        if (_mobState.IsIncapacitated(viewer))
            return false;

        // Проверяем, видит ли смотрящий цель
        if (useFov && !_fov.IsInFov(viewer.Owner, target, fovOverride))
            return false; // Если не видит, то не считаем его как смотрящего

        if (_blinking.IsBlind(viewer, true))
            return false;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(viewer, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return false;

        return true;
    }
}
