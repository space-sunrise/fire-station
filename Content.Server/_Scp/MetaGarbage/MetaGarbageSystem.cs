using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Scp.Misc;
using Content.Server._Sunrise.Helpers;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// Система сохранения мусора между раундами.
/// В конце раунда сохраняет мусор, который был в комплексе и спавнит его в начале следующего раунда.
/// TODO: Сохранение ржавых стен, битых лампочек
/// TODO: Статистика сохраненного говна в конце раунда
/// TODO: Переделать спавн мусора в геймрул
/// TODO: Сивар на отключение
/// TODO: Документ директору комплекса, что прошлая смена насрала(или нет)
/// </summary>
public sealed partial class MetaGarbageSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly HashSet<ProtoId<TagPrototype>> AllowedTags = [ "Trash", "MetaGarbageSavable" ];
    private static readonly HashSet<ProtoId<TagPrototype>> ForbiddenTags = [ "MetaGarbagePreventSaving" ];

    /// <summary>
    /// Сохраненный мусор, который будет передаваться из раунда в раунд.
    /// </summary>
    public readonly Dictionary<EntProtoId, List<StationMetaGarbageData>> CachedGarbage = [];

    private const float AlreadySpawnedItemsSearchRadius = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaGarbageTargetComponent, StationPostInitEvent>(OnMapInit, after:[typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RealRoundEndedMessage>(OnRoundEnded);

        InitializeDebug();
    }

    private void OnMapInit(Entity<MetaGarbageTargetComponent> ent, ref StationPostInitEvent args)
    {
        var mapPrototype = Prototype(ent);
        if (mapPrototype == null)
            return;

        if (!CachedGarbage.TryGetValue(mapPrototype, out var list))
            return;

        var mapId = GetStationMapId(args.Station);
        _random.Shuffle(list);
        var reducedGarbage = _helpers.GetPercentageOfHashSet(list, ent.Comp.SpawnPercent).ToList();

        foreach (var data in reducedGarbage)
        {
            var coords = new MapCoordinates(data.Position, mapId);

            if (IsItemAlreadySpawned(data.Prototype, coords, out var found) && !data.Replace)
                continue;

            if (data.Replace)
                Del(found);

            var item = Spawn(data.Prototype, coords, rotation: data.Rotation);
            TryAddLiquid(item, data.LiquidData);

            Log.Info($"Spawned {data.Prototype}|{item} at {data.Position} on map {mapId}|{Name(ent)}");
        }

        Log.Info($"Spawned {reducedGarbage.Count}/{list.Count} items");
        PrintDebugInfo(ent);
    }

    private void OnRoundEnded(RealRoundEndedMessage args)
    {
        var query = EntityQueryEnumerator<MetaGarbageTargetComponent, StationDataComponent>();

        while (query.MoveNext(out var uid, out var metaGarbage, out _))
        {
            var stationPrototype = Prototype(uid);

            if (stationPrototype == null)
                return;

            // Вычищаем прошлые данные о мусоре на данной карте и собираем их заново
            CachedGarbage.Remove(stationPrototype);

            // Сохраняем новые данные
            CollectGarbage((uid, metaGarbage), stationPrototype);
            PrintDebugInfo(uid);
        }
    }

    private void CollectGarbage(Entity<MetaGarbageTargetComponent> station, EntProtoId stationPrototype)
    {
        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();

        var debugCount = 0;

        while (query.MoveNext(out var uid, out var tag, out var xform))
        {
            if (!IsValidEntityToSave(uid, tag))
                continue;

            var itemStation = _station.GetOwningStation(uid, xform);
            if (station != itemStation)
                continue;

            var proto = Prototype(uid);
            if (proto == null)
                continue;

            if (!TryCheckSolution(station, uid, out var solution))
                continue;

            SaveEntity(xform, stationPrototype, proto, solution);
            debugCount++;
        }

        Log.Info($"Saved {debugCount} trash items");
    }

    private bool IsValidEntityToSave(EntityUid uid, TagComponent tag)
    {
        if (!_tag.HasAnyTag(tag, AllowedTags))
            return false;

        if (_tag.HasAnyTag(tag, ForbiddenTags))
            return false;

        if (HasComp<InsideEntityStorageComponent>(uid))
            return false;

        if (_container.IsEntityInContainer(uid))
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет реагенты внутри сущности.
    /// Если найдены запрещенные реагенты с шансом не дает сущности сохраниться.
    /// Если все ок - возвращает информацию о реагентах. Она может быть нулл
    /// </summary>
    private bool TryCheckSolution(Entity<MetaGarbageTargetComponent> station,
        EntityUid uid,
        out Dictionary<string, MetaGarbageSolutionProxy>? data)
    {
        data = null;

        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionContainer))
            return true;

        data = [];

        // Собираем данные о реагента
        foreach (var container in solutionContainer.Containers)
        {
            if (!_solution.TryGetSolution((uid, solutionContainer), container, out var targetSolution))
                continue;

            // Проверяем наличие специальных реагентов, количество которых мы хотим сократить
            foreach (var (reagentProto, probability) in station.Comp.ReagentSaveModifiers)
            {
                var reagent = new ReagentId(reagentProto, null);

                if (!targetSolution.Value.Comp.Solution.TryGetReagent(reagent, out _))
                    continue;

                // Если не повезло - даем сигнал, что сущность не нужно сохранять
                if (!_random.Prob(probability))
                    return false;
            }

            var solution = targetSolution.Value.Comp.Solution;
            var liquidData = new MetaGarbageSolutionProxy(ReagentToProxy(solution.Contents));
            data[container] = liquidData;
        }

        return true;
    }

    /// <summary>
    /// Сохраняет сущность в словарь для последующего спавна
    /// </summary>
    private void SaveEntity(TransformComponent xform, EntProtoId stationPrototype, EntProtoId targetProto, Dictionary<string, MetaGarbageSolutionProxy>? liquid = null)
    {
        // Сохраняем данные о мусоре в список для спавна в следующем раунде.
        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

        // Добавляем в словарь данные.
        // Ключ - айди прототипа карты, чтобы разные карты имели разный набор мусора с прошлых смен
        // Значение - список мусора, который сохранен для данной карты.
        if (CachedGarbage.TryGetValue(stationPrototype, out var list))
            list.Add(new StationMetaGarbageData(targetProto, position, rotation, liquid));
        else
            CachedGarbage[stationPrototype] = [new StationMetaGarbageData(targetProto, position, rotation, liquid)];
    }

    /// <summary>
    /// Пытается добавить реагенты в сущность, если они у нее были в прошлом раунде.
    /// Вычищает стандартные реагенты из сущности, если они там есть.
    /// </summary>
    private bool TryAddLiquid(EntityUid uid, Dictionary<string, MetaGarbageSolutionProxy>? data)
    {
        if (data == null)
            return false;

        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionContainer))
            return false;

        foreach (var (container, liquidData) in data)
        {
            var solution = new Solution(ProxyToReagent(liquidData.Contents));

            _solution.EnsureAllSolutions((uid, solutionContainer));

            if (!_solution.EnsureSolutionEntity((uid, solutionContainer),
                    container,
                    out _,
                    out var solutionEntity))
                continue;

            _solution.RemoveAllSolution(solutionEntity.Value);
            _solution.AddSolution(solutionEntity.Value, solution);

            var ev = new SolutionChangedEvent(solutionEntity.Value);
            RaiseLocalEvent(uid, ref ev);
        }

        return true;
    }

    /// <summary>
    /// Получает айди карты, на которой находится станция.
    /// </summary>
    private MapId GetStationMapId(Entity<StationDataComponent> ent)
    {
        foreach (var grid in ent.Comp.Grids)
        {
            var id = Transform(grid).MapID;

            if (id != MapId.Nullspace)
                return id;
        }

        // Сюда доходить не должно
        var fallback = Transform(ent).MapID;
        Log.Error($"Cannot find station map id, using fallback id: {fallback}");
        return fallback;
    }

    /// <summary>
    /// Проверяет, присутствует ли данный предмет на заданных координатах.
    /// Помогает избежать дублирования замапленных предметов.
    /// </summary>
    private bool IsItemAlreadySpawned(EntProtoId proto, MapCoordinates coords, [NotNullWhen(true)] out EntityUid? found)
    {
        found = null;
        foreach (var ent in _lookup.GetEntitiesInRange(coords, AlreadySpawnedItemsSearchRadius))
        {
            var prototype = Prototype(ent);
            if (prototype == null)
                continue;

            if (prototype == proto)
            {
                found = ent;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Конвертирует <seealso cref="ReagentQuantity"/> в <seealso cref="MetaGarbageReagentQuantityProxy"/>
    /// </summary>
    private static List<MetaGarbageReagentQuantityProxy> ReagentToProxy(List<ReagentQuantity> list)
    {
        List<MetaGarbageReagentQuantityProxy> toReturn = [];

        foreach (var quantity in list)
        {
            toReturn.Add(new MetaGarbageReagentQuantityProxy(quantity.Reagent, quantity.Quantity));
        }

        return toReturn;
    }

    /// <summary>
    /// Конвертирует <seealso cref="MetaGarbageReagentQuantityProxy"/> в <seealso cref="ReagentQuantity"/>
    /// </summary>
    private static List<ReagentQuantity> ProxyToReagent(List<MetaGarbageReagentQuantityProxy> list)
    {
        List<ReagentQuantity> toReturn = [];

        foreach (var quantity in list)
        {
            toReturn.Add(new ReagentQuantity(quantity.Reagent, quantity.Quantity));
        }

        return toReturn;
    }
}
