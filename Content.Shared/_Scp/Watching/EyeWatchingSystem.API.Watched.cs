using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    /// <summary>
    /// Получает всех зрителей для конкретной сущности, которые подходят заданным условиям.
    /// </summary>
    /// <param name="target">Цель, для которой ищутся зрители</param>
    /// <param name="watchers">Количество зрителей</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="flags">Флаги для поиска зрителей</param>
    /// <param name="checkProximity">Будет ли проверять тип линии видимости</param>
    /// <param name="useFov">Будет ли проверять FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол для FOV зрителя</param>
    /// <returns>Найден ли хоть один зритель</returns>
    public bool TryGetWatchers(EntityUid target,
        [NotNullWhen(true)] out int? watchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
    {
        watchers = null;

        using var realWatchers = ListPoolEntity<BlinkableComponent>.Rent();
        if (!TryGetWatchers(target, realWatchers.Value, type, flags, checkProximity, useFov, useTimeCompensation, checkBlinking, fovOverride))
            return false;

        watchers = realWatchers.Value.Count;
        return true;
    }

    /// <summary>
    /// Получает всех зрителей для конкретной сущности, которые подходят заданным условиям.
    /// </summary>
    /// <param name="target">Цель, для которой ищутся зрители</param>
    /// <param name="realWatchers">Список зрителей, который будет наполнен методом</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="flags">Флаги для поиска зрителей</param>
    /// <param name="checkProximity">Будет ли проверять тип линии видимости</param>
    /// <param name="useFov">Будет ли проверять FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол для FOV зрителя</param>
    /// <returns>Найден ли хоть один зритель</returns>
    public bool TryGetWatchers(EntityUid target,
        List<Entity<BlinkableComponent>> realWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
    {
        using var potentialWatchers = HashSetPoolEntity<BlinkableComponent>.Rent();
        _lookup.GetEntitiesInRange(Transform(target).Coordinates, SeeRange, potentialWatchers.Value, flags);

        return TryGetWatchersFrom(target,
            realWatchers,
            potentialWatchers.Value,
            type,
            checkProximity,
            useFov,
            useTimeCompensation,
            checkBlinking,
            fovOverride);
    }

    /// <summary>
    /// Получает всех зрителей для конкретной сущности из заранее заготовленного списка потенциальных зрителей, которые подходят заданным условиям.
    /// </summary>
    /// <param name="target">Цель, для которой ищутся зрители</param>
    /// <param name="realWatchers">Список зрителей, который будет наполнен методом</param>
    /// <param name="potentialWatchers">Заранее заготовленный список потенциальных зрителей, среди которых будет поиск</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="checkProximity">Будет ли проверять тип линии видимости</param>
    /// <param name="useFov">Будет ли проверять FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол для FOV зрителя</param>
    /// <returns>Найден ли хоть один зритель</returns>
    public bool TryGetWatchersFrom(EntityUid target,
        List<Entity<BlinkableComponent>> realWatchers,
        ICollection<Entity<BlinkableComponent>> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
    {
        foreach (var viewer in potentialWatchers)
        {
            if (!IsWatchedBy(target, viewer, type, checkProximity, useFov, useTimeCompensation, checkBlinking, fovOverride))
                continue;

            realWatchers.Add(viewer);
        }

        return realWatchers.Count != 0;
    }

    /// <summary>
    /// Проверяет, есть ли хоть один зритель для целевой сущности.
    /// Более оптимизированный вариант, который прерывает свое выполнение при найденном результате
    /// </summary>
    /// <param name="target">Цель для поиска зрителей</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="flags">Флаги для поиска зрителей в радиусе видимости</param>
    /// <param name="checkProximity">Будет ли проверяться тип линии видимости?</param>
    /// <param name="useFov">Будет ли проверять FOV зрителя?</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол FOV зрителя</param>
    /// <returns>Найден ли хоть один зритель для цели</returns>
    public bool IsWatchedByAny(EntityUid target,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
    {
        using var potentialWatchers = HashSetPoolEntity<BlinkableComponent>.Rent();
        _lookup.GetEntitiesInRange(Transform(target).Coordinates, SeeRange, potentialWatchers.Value, flags);

        foreach (var viewer in potentialWatchers.Value)
        {
            if (!IsWatchedBy(target, viewer, type, useFov, checkProximity, useTimeCompensation, checkBlinking, fovOverride))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, смотри ли потенциальный зритель на цель.
    /// </summary>
    /// <param name="target">Цель для проверки</param>
    /// <param name="potentialViewer">Потенциальный зритель, который проверяется</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="checkProximity">Будет ли проверяться тип линии видимости</param>
    /// <param name="useFov">Будет ли проверять FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно задать другой угол FOV зрителя</param>
    /// <returns>Смотрит ли потенциальный зритель на цель.</returns>
    public bool IsWatchedBy(EntityUid target,
        EntityUid potentialViewer,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
    {
        if (!CanBeWatched(potentialViewer, target))
            return false;

        if (checkProximity && !IsInProximity(potentialViewer, target, type))
            return false;

        if (!CanSee(potentialViewer, target, useFov, useTimeCompensation, checkBlinking, fovOverride))
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет, может ли цель вообще быть увидена смотрящим
    /// </summary>
    /// <remarks>
    /// Проверка заключается в поиске базовых компонентов, без которых Watching система не будет работать
    /// </remarks>
    /// <param name="viewer">Смотрящий, который в теории может увидеть цель</param>
    /// <param name="target">Цель, которую мы проверяем на возможность быть увиденной смотрящим</param>
    /// <returns>Да/нет</returns>
    public bool CanBeWatched(EntityUid viewer, EntityUid target)
    {
        if (!_blinkableQuery.HasComp(viewer))
            return false;

        if (viewer == target)
            return false;

        if (_insideStorageQuery.HasComp(viewer))
            return false;

        if (_mobStateQuery.TryComp(viewer, out var mobState) && _mobState.IsIncapacitated(viewer, mobState))
            return false;

        return true;
    }
}
