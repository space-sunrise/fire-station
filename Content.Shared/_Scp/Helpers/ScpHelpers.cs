using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Watching;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Helpers;

// TODO: Использовать оптимизации GC после внедрения их в EyeWatchingSystem

public sealed class ScpHelpers : EntitySystem
{
    [Dependency] private readonly EyeWatchingSystem _watching = default!;

    /// <summary>
    /// Получает суммарное количество реагента в зоне видимости сущности.
    /// Возвращает количество реагентов.
    /// </summary>
    public FixedPoint2 GetAroundSolutionVolume(EntityUid uid,
        ProtoId<ReagentPrototype> reagent,
        in List<EntityUid> puddleList,
        LineOfSightBlockerLevel lineOfSight = LineOfSightBlockerLevel.Transparent)
    {
        FixedPoint2 total = 0;
        var puddles = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(uid, lineOfSight);

        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var solution = puddle.Comp.Solution.Value.Comp.Solution;

            foreach (var (reagentId, quantity) in solution.Contents)
            {
                if (reagentId.Prototype != reagent)
                    continue;

                puddleList.Add(puddle);
                total += quantity;
            }
        }

        return total;
    }

    /// <summary>
    /// Получает суммарное количество реагента в зоне видимости сущности.
    /// Возвращает количество реагентов.
    /// </summary>
    public FixedPoint2 GetAroundSolutionVolume(EntityUid uid,
        ProtoId<ReagentPrototype> reagent,
        LineOfSightBlockerLevel lineOfSight = LineOfSightBlockerLevel.Transparent)
    {
        FixedPoint2 total = 0;
        var puddles = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(uid, lineOfSight);

        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var solution = puddle.Comp.Solution.Value.Comp.Solution;

            foreach (var (reagentId, quantity) in solution.Contents)
            {
                if (reagentId.Prototype != reagent)
                    continue;

                total += quantity;
            }
        }

        return total;
    }

    public bool IsAroundSolutionVolumeGreaterThan(EntityUid uid,
        ProtoId<ReagentPrototype> reagent,
        FixedPoint2 required,
        LineOfSightBlockerLevel lineOfSight = LineOfSightBlockerLevel.Transparent)
    {
        FixedPoint2 total = 0;
        var puddles = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(uid, lineOfSight);

        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var solution = puddle.Comp.Solution.Value.Comp.Solution;

            foreach (var (reagentId, quantity) in solution.Contents)
            {
                if (reagentId.Prototype != reagent)
                    continue;

                total += quantity;

                if (total >= required)
                    return true;

            }
        }

        return false;
    }
}
