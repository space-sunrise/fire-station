using Content.Shared.Research;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Helpers;

/// <summary>
/// Система, позволяющая просчитать стоимость технологии исходя из относительных модификаторов.
/// </summary>
[PublicAPI]
public sealed class ResearchPointsHelper : EntitySystem
{
    public static readonly ProtoId<ResearchPointPrototype> DefaultPoint = "Default";
    public static readonly ProtoId<ResearchPointPrototype> ScpPoint = "Scp";

    /// <summary>
    /// Сохраненные значения стоимости технологий.
    /// Кешируем здесь, чтобы оптимизировать подсчет очков, так как стоимость технологий не меняется по ходу раунда
    /// </summary>
    private static readonly Dictionary<ProtoId<TechnologyPrototype>, Dictionary<ProtoId<ResearchPointPrototype>, int>>
        CachedCost = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(_ => CachedCost.Clear());
    }

    /// <summary>
    /// Проверяет, возможно ли купить технологию за данное количество очков
    /// </summary>
    /// <param name="tech">Прототип технологии, для которой идет проверка</param>
    /// <param name="totalPoints">Количество доступных для покупки очков</param>
    /// <returns>Получится или не получится купить</returns>
    public static bool CanBuy(TechnologyPrototype tech, Dictionary<ProtoId<ResearchPointPrototype>, int> totalPoints)
    {
        var cost = GetPoints(tech);
        foreach (var (researchPointType, requiredAmount) in cost)
        {
            if (!totalPoints.TryGetValue(researchPointType, out var point))
                return false;

            if (point < requiredAmount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Подсчитывает стоимость технологии, автоматически просчитывая нужные параметры.
    /// </summary>
    /// <param name="tech">Прототип технологии, для которой идет подсчет</param>
    /// <returns>Словарь стоимости технологии, где ключ - тип очков, а значение - требуемое количество</returns>
    public static Dictionary<ProtoId<ResearchPointPrototype>, int> GetPoints(TechnologyPrototype tech)
    {
        // Нашли сохраненное значение - возвращаем его. Иначе считаем снова
        if (CachedCost.TryGetValue(tech.ID, out var cost))
            return cost;

        // Новый словарь, чтобы не изменять значение в прототипе
        var computedCost = new Dictionary<ProtoId<ResearchPointPrototype>, int>(tech.CostList);

        if (!computedCost.ContainsKey(DefaultPoint) && tech.Cost != 0)
            computedCost[DefaultPoint] = tech.Cost;

        if (!computedCost.ContainsKey(ScpPoint) && tech.DefaultToScpScale != 0)
        {
            if (!computedCost.TryGetValue(DefaultPoint, out var defaultCost))
            {
                Logger.Error($"Technology '{tech.ID}' has no default research cost defined, but DefaultToScpScale is set to {tech.DefaultToScpScale}. Unable to compute SCP cost.");
                return computedCost;
            }

            computedCost[ScpPoint] = (int) Math.Ceiling(defaultCost * tech.DefaultToScpScale);
        }

        CachedCost[tech.ID] = computedCost;
        return computedCost;
    }
}
