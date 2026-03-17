using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    /*
     * Внутренние методы для работы внутри Update() цикла.
     * Нужны, чтобы исключить дублирование проверки Proximity(),
     * которая происходит из-за логики Simple и Full проверок в цикле.
     * По факту являются чуть исправленной копией публичных методов,
     * но с использованием WatchCandidate для переноса просчитанного BlockerLevel
     */

    private bool _TryGetAllEntitiesVisibleTo(
        Entity<TransformComponent?> ent,
        List<WatchCandidate> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Approximate)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        using var searchSet = HashSetPoolEntity<BlinkableComponent>.Rent();
        _lookup.GetEntitiesInRange(ent.Comp.Coordinates, SeeRange, searchSet.Value, flags);

        foreach (var target in searchSet.Value)
        {
            if (!_proximity.IsRightType(ent, target, type, out var blockerLevel))
                continue;

            potentialWatchers.Add(new WatchCandidate(target, blockerLevel));
        }

        return potentialWatchers.Count != 0;
    }

    private bool _TryGetWatchersFrom(EntityUid target,
        List<WatchCandidate> realWatchers,
        ICollection<WatchCandidate> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        bool checkProximity = true,
        bool useFov = true,
        float? fovOverride = null)
    {
        foreach (var viewer in potentialWatchers)
        {
            if (!IsWatchedBy(target, viewer.Viewer, type, useFov, checkProximity, fovOverride))
                continue;

            realWatchers.Add(viewer);
        }

        return realWatchers.Count != 0;
    }

    private readonly record struct WatchCandidate(Entity<BlinkableComponent> Viewer, LineOfSightBlockerLevel BlockerLevel);
}
