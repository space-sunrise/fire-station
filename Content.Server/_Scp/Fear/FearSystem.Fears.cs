using System.Linq;
using Content.Server._Scp.Shaders.Highlighting;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Components.Fears;
using Content.Shared._Scp.Watching;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly HighlightSystem _highlight = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private void InitializeFears()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<HemophobiaComponent, FearCalmDownAttemptEvent>(OnCalmDown);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target))
            return;

        var activated = ev.NewMobState == MobState.Dead;
        var toggleUsed = new ItemToggledEvent(false, activated, null);
        RaiseLocalEvent(ev.Target, ref toggleUsed);
    }

    private void UpdateHemophobia()
    {
        var query = EntityQueryEnumerator<HemophobiaComponent, FearComponent>();

        while (query.MoveNext(out var uid, out var hemophobia, out var fear))
        {
            var bloodAmount = GetAroundBloodVolume((uid, hemophobia), out var bloodList);
            var requiredBloodAmount = hemophobia.BloodRequiredPerState[fear.State];

            if (bloodAmount <= requiredBloodAmount)
                continue;

            var fearEntity = (uid, fear);

            if (!TrySetFearLevel(fearEntity, GetHemophobiaFearState(hemophobia, bloodAmount)))
                continue;

            _highlight.HighLightAll(bloodList, uid);
        }
    }

    /// <summary>
    /// Получает суммарное количество крови в зоне видимости персонажа.
    /// </summary>
    private FixedPoint2 GetAroundBloodVolume(Entity<HemophobiaComponent> ent, out List<EntityUid> bloodList)
    {
        FixedPoint2 total = 0;
        var blood = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(ent.Owner);
        bloodList = [];

        foreach (var puddle in blood)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            bloodList.Add(puddle);

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototype);
            total = allReagents
                .Where(reagent => reagent.Key.ID == ent.Comp.Reagent)
                .Aggregate(total, (current, reagent) => current + reagent.Value);
        }

        return total;
    }

    /// <summary>
    /// Получает уровень страха, соответствующий текущему количеству крови вокруг.
    /// </summary>
    private static FearState GetHemophobiaFearState(HemophobiaComponent component, FixedPoint2 bloodAmount)
    {
        var result = FearState.None;

        foreach (var kvp in component.BloodRequiredPerState.OrderBy(kv => kv.Value))
        {
            if (bloodAmount >= kvp.Value)
                result = kvp.Key;
            else
                break;
        }

        return result;
    }

    /// <summary>
    /// Проверяет, может ли сущность с гемофобией успокоиться в данный момент.
    /// Для этого рядом не должно быть большого количества крови
    /// </summary>
    private void OnCalmDown(Entity<HemophobiaComponent> ent, ref FearCalmDownAttemptEvent args)
    {
        FixedPoint2 total = 0;
        var blood = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(ent.Owner);
        var requiredBloodToCancel = ent.Comp.BloodRequiredPerState[args.NewState];

        foreach (var puddle in blood)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototype);

            total = allReagents
                .Where(reagent => reagent.Key.ID == ent.Comp.Reagent)
                .Aggregate(total, (current, reagent) => current + reagent.Value);

            if (total > requiredBloodToCancel)
            {
                args.Cancel();
                return;
            }
        }
    }
}
