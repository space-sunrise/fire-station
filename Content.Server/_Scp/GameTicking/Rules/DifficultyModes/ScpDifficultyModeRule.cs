using System.Linq;
using Content.Server._Sunrise.Helpers;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Item;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.GameTicking.Rules.DifficultyModes;

public sealed class ScpDifficultyModeRule : GameRuleSystem<ScpDifficultyModeRuleComponent>
{
    [Dependency] private readonly StationJobsSystem _jobs = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid,
        ScpDifficultyModeRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var targetStation))
            return;

        // Проходимся по всем работкам и сцп-предметами, устанавливая текущие правила игры
        // Слоты на неподходящие работы закрываются, а предметы удаляются

        DealWithPlayableScp(component, targetStation.Value);
        DealWithItems(component, targetStation.Value);
    }

    private void DealWithPlayableScp(ScpDifficultyModeRuleComponent component, EntityUid targetStation)
    {
        foreach (var (classification, slots) in component.ScpSlots)
        {
            if (slots == ScpDifficultyModeRuleComponent.UnlimitedSlotsFlag)
                continue;

            // Получаем все работки SCP, которые соответствуют текущему классу содержания
            var matchingScp = _prototype.EnumeratePrototypes<JobPrototype>()
                .Where(proto => IsMatchingScpJob(classification, proto, component.PlayableWhitelist, component.PlayableBlacklist));

            // Подсчитываем, сколько будет доступно слотов
            var targetSlots = slots.Next(_random);

            // Устанавливаем для каждого подходящего SCP количество слотов
            foreach (var job in matchingScp)
            {
                _jobs.TrySetJobSlot(targetStation, job, targetSlots);
            }
        }
    }

    private void DealWithItems(ScpDifficultyModeRuleComponent component, EntityUid targetStation)
    {
        // Находим все неподходящие для данного режима предметы
        var inappropriateItems = _helpers.GetAll<ScpComponent, ItemComponent>()
            .Where(item =>
                !IsMatchingItem((item, item.Comp1), component.ItemWhitelist, component.ItemBlacklist, targetStation));

        // И удаляем их
        foreach (var item in inappropriateItems)
        {
            QueueDel(item);
        }
    }

    /// <summary>
    /// Проверяет, является ли данная работа SCP и подходит ли под требования режима
    /// </summary>
    /// <returns>Да/Нет</returns>
    private bool IsMatchingScpJob(Classification classification, JobPrototype job, ComponentRegistry? whitelist, ComponentRegistry? blacklist)
    {
        if (job.JobEntity == null)
            return false;

        if (!_prototype.TryIndex<EntityPrototype>(job.JobEntity, out var entity))
            return false;

        // Реализация вайтлиста. Так как в вайтлисте будет перечисление компонентов, которые будут представлять сцп
        // То нам нужно, чтобы хотя бы один совпал
        if (whitelist != null && !entity.Components.Any(whitelist.Contains))
            return false;

        // Обратная ситуация с блеклистом. Нужно, чтобы не совпал ни один.
        // Следовательно, делаем возврат, если найден хоть один
        if (blacklist != null && entity.Components.Any(blacklist.Contains))
            return false;

        if (!entity.Components.TryGetComponent("Scp", out var component) || component is not ScpComponent scpComponent)
            return false;

        if (scpComponent.Class != classification)
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет, подходит ли данный предмет под требования режима
    /// </summary>
    /// <returns>Да/Нет</returns>
    private bool IsMatchingItem(Entity<ScpComponent> item, EntityWhitelist? whitelist, EntityWhitelist? blacklist, EntityUid targetStation)
    {
        if (!_whitelist.CheckBoth(item, blacklist, whitelist))
            return false;

        var station = _station.GetOwningStation(item);

        if (station != targetStation)
            return false;

        return true;
    }
}
