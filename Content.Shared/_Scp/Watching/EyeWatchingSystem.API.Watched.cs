using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;

namespace Content.Shared._Scp.Watching;

// TODO: Унифицировать название переменных realWatchers/realViewers + potentialWatchers/potentialViewers
public sealed partial class EyeWatchingSystem
{
    public bool TryGetWatchers(EntityUid target,
        [NotNullWhen(true)] out int? watchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
    {
        watchers = null;

        using var realWatchers = ListPoolEntity<BlinkableComponent>.Rent();
        if (!TryGetWatchers(target, realWatchers.Value, type, flags, useFov, checkProximity, fovOverride))
            return false;

        watchers = realWatchers.Value.Count;
        return true;
    }

    public bool TryGetWatchers(EntityUid target,
        List<Entity<BlinkableComponent>> realWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
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
            fovOverride);
    }

    public bool TryGetWatchersFrom(EntityUid target,
        List<Entity<BlinkableComponent>> realWatchers,
        ICollection<Entity<BlinkableComponent>> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
    {
        foreach (var viewer in potentialWatchers)
        {
            if (!IsWatchedBy(target, viewer, type, useFov, checkProximity, fovOverride))
                continue;

            realWatchers.Add(viewer);
        }

        return realWatchers.Any();
    }

    public bool IsWatchedByAny(EntityUid target,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
    {
        using var potentialWatchers = HashSetPoolEntity<BlinkableComponent>.Rent();
        _lookup.GetEntitiesInRange(Transform(target).Coordinates, SeeRange, potentialWatchers.Value, flags);

        foreach (var viewer in potentialWatchers.Value)
        {
            if (!IsWatchedBy(target, viewer, type, useFov, checkProximity, fovOverride))
                continue;

            return true;
        }

        return false;
    }

    public bool IsWatchedBy(EntityUid target,
        EntityUid potentialViewer,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
    {
        if (!CanBeWatched(potentialViewer, target))
            return false;

        if (checkProximity && !IsInProximity(potentialViewer, target, type))
            return false;

        if (!CanSee(potentialViewer, target, useFov, fovOverride))
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
    public bool CanBeWatched(Entity<BlinkableComponent?> viewer, EntityUid target)
    {
        if (!_blinkableQuery.Resolve(viewer.Owner, ref viewer.Comp, false))
            return false;

        if (viewer.Owner == target)
            return false;

        if (_insideStorageQuery.HasComp(viewer))
            return false;

        if (_mobStateQuery.TryComp(viewer, out var mobState) && _mobState.IsIncapacitated(viewer, mobState))
            return false;

        return true;
    }
}
