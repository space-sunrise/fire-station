using System.Text;

namespace Content.Server._Scp.MetaGarbage;

public sealed partial class MetaGarbageSystem
{
    private void InitializeDebug()
    {
#if DEBUG
        Log.Level = LogLevel.Debug;
#else
        Log.Level = LogLevel.Info;
#endif
    }

    /// <summary>
    /// Логгирует информацию для отладки системы сохранения мусора.
    /// </summary>
    private void PrintDebugInfo(EntityUid station)
    {
        var proto = Prototype(station);
        if (proto == null)
            return;

        foreach (var (stationProto, dataList) in CachedGarbage)
        {
            if (stationProto != proto)
                continue;

            foreach (var data in dataList)
            {
                Log.Info($"{data.Prototype} - Liquid: {GetDebugLiquidInfo(data.LiquidData)} | Replace: {data.Replace} Container: {data.ContainerName} BulbState: {data.BulbState}");
            }
        }
    }

    /// <summary>
    /// Собирает в единую строку данные о жидкости внутри мусора, с которым работает система.
    /// Позволяет удобно выводить информацию о жидкости.
    /// </summary>
    private static string GetDebugLiquidInfo(Dictionary<string, MetaGarbageSolutionProxy>? data)
    {
        if (data == null)
            return string.Empty;

        StringBuilder debugInfo = new();

        foreach (var (container, solution) in data)
        {
            debugInfo.Append(container);
            debugInfo.Append(" + ");
            debugInfo.Append(GetSolutionContents(solution.Contents));
            debugInfo.Append(" ");
        }

        return debugInfo.ToString();
    }

    /// <summary>
    /// Собирает данные о реагентах в одну строку для вывода в консоль
    /// </summary>
    private static string GetSolutionContents(List<MetaGarbageReagentQuantityProxy> contents)
    {
        StringBuilder debugInfo = new();

        foreach (var content in contents)
        {
            debugInfo.Append("[");
            debugInfo.Append(content.Quantity);
            debugInfo.Append(" ");
            debugInfo.Append(content.Reagent);
            debugInfo.Append("]");
        }

        return debugInfo.ToString();
    }
}
