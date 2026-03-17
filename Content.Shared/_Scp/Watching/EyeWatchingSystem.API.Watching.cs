using System.Linq;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    public bool TryGetWatchingTargets<T>(EntityUid watcher,
        List<Entity<T>> targets,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate,
        bool checkProximity = true,
        bool useFov = true,
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
            fovOverride);
    }

    public bool TryGetWatchingTargetsFrom<T>(EntityUid watcher,
        List<Entity<T>> targets,
        ICollection<Entity<T>> potentialTargets,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
        where T : IComponent
    {
        foreach (var target in potentialTargets)
        {
            if (!IsWatchedBy(target, watcher, type, useFov, checkProximity, fovOverride))
                continue;

            targets.Add(target);
        }

        return targets.Any();
    }
}
