using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    /// <summary>
    /// Получает и возвращает на кого в данный момент смотрит зритель.
    /// </summary>
    /// <param name="watcher">Зритель, для которого идут проверки</param>
    /// <param name="targets">Список целей, в который метод занесет всех, на кого смотрит зритель</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="flags">Флаги для поиска целей</param>
    /// <param name="checkProximity">Будет ли проверяться тип линии видимости</param>
    /// <param name="useFov">Будет ли проверяться FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно поставить другой угол FOV зрителя</param>
    /// <typeparam name="T">Компонент, который должен быть у целей</typeparam>
    /// <returns>Найдена ли хоть одна цель</returns>
    public bool TryGetWatchingTargets<T>(EntityUid watcher,
        List<Entity<T>> targets,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
        where T : IComponent
    {
        using var potentialTargets = HashSetPoolEntity<T>.Rent();
        _lookup.GetEntitiesInRange(Transform(watcher).Coordinates, SeeRange, potentialTargets.Value, flags);

        return TryGetWatchingTargetsFrom(watcher,
            targets,
            potentialTargets.Value,
            type,
            checkProximity,
            useFov,
            useTimeCompensation,
            checkBlinking,
            fovOverride);
    }

    /// <summary>
    /// Получает и возвращает на кого в данный момент смотрит зритель.
    /// Использует заранее заготовленный список целей для поиска реальных целей
    /// </summary>
    /// <param name="watcher">Зритель, для которого идут проверки</param>
    /// <param name="targets">Список целей, в который метод занесет всех, на кого смотрит зритель</param>
    /// <param name="type">Требуемый тип линии видимости</param>
    /// <param name="potentialTargets">Заранее заготовленный список целей из которых будет производиться поиск.</param>
    /// <param name="checkProximity">Будет ли проверяться тип линии видимости</param>
    /// <param name="useFov">Будет ли проверяться FOV зрителя</param>
    /// <param name="useTimeCompensation">Будет ли использоваться компенсация времени? Нужно для передвижения SCP-173</param>
    /// <param name="checkBlinking">Будет ли проводиться проверка на моргание?</param>
    /// <param name="fovOverride">Если нужно поставить другой угол FOV зрителя</param>
    /// <typeparam name="T">Компонент, который должен быть у целей</typeparam>
    /// <returns>Найдена ли хоть одна цель</returns>
    public bool TryGetWatchingTargetsFrom<T>(EntityUid watcher,
        List<Entity<T>> targets,
        ICollection<Entity<T>> potentialTargets,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        bool useTimeCompensation = false,
        bool checkBlinking = true,
        float? fovOverride = null)
        where T : IComponent
    {
        foreach (var target in potentialTargets)
        {
            if (!IsWatchedBy(target,
                    watcher,
                    type,
                    checkProximity,
                    useFov,
                    useTimeCompensation,
                    checkBlinking,
                    fovOverride))
                continue;

            targets.Add(target);
        }

        return targets.Count != 0;
    }
}
