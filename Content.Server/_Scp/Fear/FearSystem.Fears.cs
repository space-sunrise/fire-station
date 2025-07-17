using Content.Server._Scp.Shaders.Highlighting;
using Content.Server._Sunrise.Mood;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Components.Fears;
using Content.Shared._Scp.Watching;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly HighlightSystem _highlight = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;

    private const string MoodSomeoneDiedOnMyEyes = "FearSomeoneDiedOnMyEyes";

    private const string MoodHemophobicBleeding = "FearHemophobicBleeding";
    private const string MoodHemophobicSeeBlood = "FearHemophobicSeeBlood";

    private void InitializeFears()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<HemophobiaComponent, FearCalmDownAttemptEvent>(OnCalmDown);
    }

    /// <summary>
    /// Обрабатывает событие смерти персонажей.
    /// Если персонаж умер, он становится страшным.
    /// Если воскрес, перестает
    /// </summary>
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target))
            return;

        var activated = ev.NewMobState == MobState.Dead;
        var toggleUsed = new ItemToggledEvent(false, activated, null);
        RaiseLocalEvent(ev.Target, ref toggleUsed);

        if (!activated)
            return;

        var whoSaw = _watching.GetAllEntitiesVisibleTo<MoodComponent>(ev.Target);

        foreach (var uid in whoSaw)
        {
            AddNegativeMoodEffect(uid, MoodSomeoneDiedOnMyEyes);
        }
    }

    /// <summary>
    /// Обрабатывает страх гемофобов.
    /// Проверяет количество окружающей крови и в зависимости от этого пугает персонажей.
    /// </summary>
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

            _highlight.NetHighlightAll(bloodList, uid);
            AddNegativeMoodEffect(uid, MoodHemophobicSeeBlood);
        }
    }

    /// <summary>
    /// Получает суммарное количество крови в зоне видимости персонажа.
    /// </summary>
    private FixedPoint2 GetAroundBloodVolume(Entity<HemophobiaComponent> ent, out HashSet<EntityUid> bloodList)
    {
        FixedPoint2 total = 0;
        var blood = _watching.GetAllEntitiesVisibleTo<PuddleComponent>(ent.Owner);
        bloodList = [];

        foreach (var puddle in blood)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var solution = puddle.Comp.Solution.Value.Comp.Solution;

            foreach (var (reagentId, quantity) in solution.Contents)
            {
                if (reagentId.Prototype != ent.Comp.Reagent)
                    continue;

                bloodList.Add(puddle);
                total += quantity;
            }
        }

        return total;
    }

    /// <summary>
    /// Получает уровень страха, соответствующий текущему количеству крови вокруг.
    /// </summary>
    private static FearState GetHemophobiaFearState(HemophobiaComponent component, FixedPoint2 bloodAmount)
    {
        var result = FearState.None;

        foreach (var kvp in component.SortedBloodRequiredPerState)
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
        var bloodAmount = GetAroundBloodVolume(ent, out _);
        var requiredBloodToCancel = ent.Comp.BloodRequiredPerState[args.NewState];

        if (bloodAmount > requiredBloodToCancel)
            args.Cancel();
    }
}
