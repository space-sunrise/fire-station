using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._Scp.Misc;
using Content.Server.Database;
using Content.Server.Light.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// Система сохранения мусора между раундами.
/// В конце раунда сохраняет мусор, который был в комплексе и спавнит его в начале следующего раунда.
/// </summary>
public sealed partial class MetaGarbageSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly LightBulbSystem _bulb = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private static readonly HashSet<ProtoId<TagPrototype>> AllowedTags = [ "Trash", "MetaGarbageSavable" ];
    private static readonly HashSet<ProtoId<TagPrototype>> ForbiddenTags = [ "MetaGarbagePreventSaving" ];
    private static readonly ProtoId<TagPrototype> ReplaceTag = "MetaGarbageReplace";
    private static readonly ProtoId<TagPrototype> ContainerAllowedTag = "MetaGarbageCanBeSpawnedInContainer";
    private static readonly string SaveDirectory = "data/meta_garbage";

    /// <summary>
    /// Сохраненный мусор, который будет передаваться из раунда в раунд.
    /// Ключ - прототип комплекса, к которому привязан мусор.
    /// Значение - список данных о мусоре, который был сохранен.
    /// </summary>
    public Dictionary<EntProtoId, List<StationMetaGarbageData>> CachedGarbage { get; private set; } = [];

    /// <summary>
    /// Радиус поиска аналогичных сущностей, который используется для поиска аналогичных предметов на месте спавна.
    /// Нужен, чтобы не спавнить замапленный на карте мусор, дублируя его.
    /// </summary>
    private const float AlreadySpawnedItemsSearchRadius = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaGarbageTargetComponent, StationPostInitEvent>(OnMapInit, after:[typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RealRoundEndedMessage>(OnRoundEnded);

        InitializeCCVars();
        InitializeDebug();
    }

    private void OnMapInit(Entity<MetaGarbageTargetComponent> ent, ref StationPostInitEvent args)
    {
        var stationComp = args.Station.Comp;
        _ = LoadFromDbAndSpawn(ent, stationComp);
    }

    private void OnRoundEnded(RealRoundEndedMessage args)
    {
        if (!TrySaveGarbage())
            return;

        try
        {
            Directory.CreateDirectory(SaveDirectory);
        }
        catch (Exception e)
        {
            Log.Error($"[MetaGarbage] Failed to create save directory: {e}");
            return;
        }

        // Iterate over stations that actually exist this round.
        // Iterating CachedGarbage could include stale entries from previous rounds,
        // and skipping CachedGarbage for empty stations would leave stale files on disk.
        var query = EntityQueryEnumerator<MetaGarbageTargetComponent, StationDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            var stationProto = Prototype(uid);
            if (stationProto == null)
                continue;

            var dataList = CachedGarbage.GetValueOrDefault(stationProto) ?? [];
            var json = MetaGarbageSerializer.Serialize(dataList);
            var version = comp.MapVersion;
            var savedAt = DateTime.UtcNow;

            var payload = JsonSerializer.Serialize(new MetaGarbageFileSave
            {
                MapVersion = version,
                SavedAt = savedAt,
                Data = json
            });

            var path = Path.Combine(SaveDirectory, $"{stationProto.ID}.json");

            try
            {
                File.WriteAllText(path, payload);
                Log.Info($"[MetaGarbage] Saved {dataList.Count} items to file for {stationProto.ID}");
            }
            catch (Exception e)
            {
                Log.Error($"[MetaGarbage] Failed to write save file for {stationProto.ID}: {e}");
            }

            // Fire-and-forget DB backup with explicit fault logging
            _ = _db.SaveMetaGarbageAsync(stationProto.ID, json, version, savedAt)
                .ContinueWith(t => Log.Error($"[MetaGarbage] DB save failed for {stationProto.ID}: {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    private async Task LoadFromDbAndSpawn(Entity<MetaGarbageTargetComponent> ent, StationDataComponent stationComp)
    {
        var proto = Prototype(ent);
        if (proto == null)
            return;

        if (!CachedGarbage.ContainsKey(proto))
        {
            var loaded = await TryLoadFromFile(proto, ent.Comp.MapVersion)
                         || await TryLoadFromDb(proto, ent.Comp.MapVersion);

            if (!loaded)
                Log.Debug($"[MetaGarbage] No valid saved data for {proto.ID}");
        }

        if (CachedGarbage.ContainsKey(proto))
            TrySpawnGarbage((ent, ent.Comp, stationComp));
    }

    private async Task<bool> TryLoadFromFile(EntityPrototype proto, int currentMapVersion)
    {
        var filePath = Path.Combine(SaveDirectory, $"{proto.ID}.json");
        if (!File.Exists(filePath))
            return false;

        try
        {
            var payload = JsonSerializer.Deserialize<MetaGarbageFileSave>(
                await File.ReadAllTextAsync(filePath));

            if (payload == null)
                return false;

            if (payload.MapVersion != currentMapVersion)
            {
                Log.Warning($"[MetaGarbage] Map version mismatch for {proto.ID} (file): " +
                            $"saved={payload.MapVersion} current={currentMapVersion}. Trying DB fallback.");
                return false;
            }

            CachedGarbage[proto] = MetaGarbageSerializer.Deserialize(payload.Data);
            Log.Info($"[MetaGarbage] Loaded {CachedGarbage[proto].Count} items from file for {proto.ID}");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"[MetaGarbage] Failed to load file for {proto.ID}: {e}. Trying DB fallback.");
            return false;
        }
    }

    private async Task<bool> TryLoadFromDb(EntityPrototype proto, int currentMapVersion)
    {
        try
        {
            var entry = await _db.GetMetaGarbageAsync(proto.ID);
            if (entry == null)
                return false;

            if (entry.MapVersion != currentMapVersion)
            {
                Log.Warning($"[MetaGarbage] Map version mismatch for {proto.ID} (DB): " +
                            $"saved={entry.MapVersion} current={currentMapVersion}. Skipping spawn.");
                return false;
            }

            CachedGarbage[proto] = MetaGarbageSerializer.Deserialize(entry.Data);
            Log.Info($"[MetaGarbage] Loaded {CachedGarbage[proto].Count} items from DB for {proto.ID}");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"[MetaGarbage] Failed to load from DB for {proto.ID}: {e}");
            return false;
        }
    }

    /// <summary>
    /// Сохраняет мусор для всех станций
    /// </summary>
    public bool TrySaveGarbage()
    {
        if (!_enableSaving)
            return false;

        var query = EntityQueryEnumerator<MetaGarbageTargetComponent, StationDataComponent>();

        while (query.MoveNext(out var uid, out var metaGarbage, out _))
        {
            var stationPrototype = Prototype(uid);
            if (stationPrototype == null)
                continue;

            // Вычищаем прошлые данные о мусоре на данной карте и собираем их заново
            CachedGarbage.Remove(stationPrototype);

            // Сохраняем новые данные
            CollectGarbage((uid, metaGarbage), stationPrototype);
            PrintDebugInfo(uid);
        }

        return true;
    }

    /// <summary>
    /// Спавнит сохраненный для переданной станции мусор.
    /// </summary>
    public bool TrySpawnGarbage(Entity<MetaGarbageTargetComponent?, StationDataComponent?> ent)
    {
        if (!_enableSpawning)
            return false;

        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        var mapPrototype = Prototype(ent);
        if (mapPrototype == null)
            return false;

        if (!CachedGarbage.TryGetValue(mapPrototype, out var list))
            return false;

        list.ShuffleRobust(_random).TakePercentage(ent.Comp1.SpawnPercent);
        var mapId = GetStationMapId((ent, ent.Comp2));

        var spawnedCount = 0;
        foreach (var data in list)
        {
            var coords = new MapCoordinates(data.Position, mapId);

            if (IsItemAlreadySpawned(data.Prototype, coords, out var found) && !data.Replace)
                continue;

            if (data.Replace)
                Del(found);

            var item = Spawn(data.Prototype, coords, rotation: data.Rotation);
            TryAddLiquid(item, data.LiquidData);
            TrySetBulbState(item, data.BulbState);
            TryInsertIntoContainer(item, coords, data.ContainerName);

            if (data.ExtraData != null)
            {
                var restoreEv = new MetaGarbageRestoreEvent(data.ExtraData);
                RaiseLocalEvent(item, ref restoreEv);
            }

            spawnedCount++;
            Log.Debug($"Spawned {data.Prototype}|{item} at {data.Position} on map {mapId}|{Name(ent)}");
        }

        Log.Info($"Spawned {spawnedCount}/{list.Count} items");
        PrintDebugInfo(ent);

        return true;
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

            // Use EntityPrototype from metadata instead of Prototype() to get the exact
            // spawned prototype ID rather than a resolved parent prototype
            var proto = MetaData(uid).EntityPrototype;
            if (proto == null)
                continue;

            if (!TryCheckSolution(station, uid, out var solution))
                continue;

            SaveEntity((uid, xform), stationPrototype, proto, solution);
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

        // Если сохранение в контейнерах разрешено - считаем сущность доступной для сохранения
        // так как ниже будут только проверки на контейнеры. Остальные проверки стоит размещать выше.
        if (_tag.HasTag(tag, ContainerAllowedTag))
            return true;

        // Проверка на контейнеры.
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
    private void SaveEntity(Entity<TransformComponent> ent, EntProtoId stationPrototype, EntProtoId targetProto, Dictionary<string, MetaGarbageSolutionProxy>? liquid = null)
    {
        // Сохраняем данные о мусоре в список для спавна в следующем раунде.
        var position = _transform.GetWorldPosition(ent.Comp);
        var rotation = _transform.GetWorldRotation(ent.Comp);
        var replace = _tag.HasTag(ent, ReplaceTag);
        var containerName = _container.TryGetOuterContainer(ent, ent.Comp, out var container) ? container.ID : null;
        LightBulbState? bulbState = TryComp<LightBulbComponent>(ent, out var bulb) ? bulb.State : null;

        var extraData = new Dictionary<string, JsonElement>();
        var saveEv = new MetaGarbageSaveEvent(extraData);
        RaiseLocalEvent(ent, ref saveEv);

        var data = new StationMetaGarbageData(targetProto, position, rotation, liquid, replace, containerName, bulbState, extraData.Count > 0 ? extraData : null);

        // Добавляем в словарь данные.
        // Ключ - айди прототипа карты, чтобы разные карты имели разный набор мусора с прошлых смен
        // Значение - список мусора, который сохранен для данной карты.
        if (CachedGarbage.TryGetValue(stationPrototype, out var list))
            list.Add(data);
        else
            CachedGarbage[stationPrototype] = [data];
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
            var prototype = MetaData(ent).EntityPrototype;
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

    /// <summary>
    /// Пытается задать состояние лампочки.
    /// Например, разбитое или сожженое состояние.
    /// </summary>
    private bool TrySetBulbState(EntityUid uid, LightBulbState? state)
    {
        if (state == null)
            return false;

        _bulb.SetState(uid, state.Value);

        Log.Debug($"Bulb`s({Name(uid)}) state changed to {state.ToString()}");
        return true;
    }

    /// <summary>
    /// Пытается найти рядом нужный контейнер и положить внутрь сущность.
    /// </summary>
    /// <param name="uid">Сущность, которую мы хотим положить</param>
    /// <param name="coords">Координаты, где искать контейнер</param>
    /// <param name="container">Название контейнера, по которому мы будем его искать</param>
    /// <returns>Получилось ли вставить сущность или нет</returns>
    private bool TryInsertIntoContainer(EntityUid uid, MapCoordinates coords, string? container)
    {
        if (string.IsNullOrEmpty(container))
            return false;

        // Проходимся по всей контейнерам близким к данным координатам.
        // И проверяем, что этот контейнер имеет нужное нам название.
        var lookup = _lookup.GetEntitiesInRange<ContainerManagerComponent>(coords, 1f);
        foreach (var ent in lookup)
        {
            foreach (var (name, comp) in ent.Comp.Containers)
            {
                if (name != container)
                    continue;

                // Проверяем, есть ли в контейнере подобная нашей сущность
                // Если есть - вытаскиваем ее, удаляем и помещаем нашу.
                if (comp.ContainedEntities.Count != 0)
                {
                    var item = EntityUid.Invalid;
                    foreach (var contained in comp.ContainedEntities)
                    {
                        if (!IsSameItem(uid, contained))
                            continue;

                        item = contained;
                        break;
                    }

                    if (item == EntityUid.Invalid)
                        continue;

                    if (_tag.HasTag(uid, ReplaceTag))
                    {
                        _container.RemoveEntity(ent, item, ent.Comp, force: true);
                        Del(item);
                    }
                }

                _container.Insert(uid, comp, force: true);

                Log.Debug($"{Name(uid)} inserted into container {container} in {Name(ent)}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет, равен ли айди прототипа у двух сущностей.
    /// </summary>
    private bool IsSameItem(EntityUid uid, EntityUid other)
    {
        var uidProto = MetaData(uid).EntityPrototype;
        var otherProto = MetaData(other).EntityPrototype;

        return uidProto == otherProto;
    }
}
