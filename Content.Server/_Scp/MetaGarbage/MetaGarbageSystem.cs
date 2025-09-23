using System.Linq;
using Content.Server._Scp.Misc;
using Content.Server._Sunrise.Helpers;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// Система сохранения мусора между раундами.
/// В конце раунда сохраняет мусор, который был в комплексе и спавнит его в начале следующего раунда.
/// TODO: Исправить дублирование предметов из-за замапленного мусора
/// TODO: Рандомизация списка мусора перед чисткой
/// TODO: Сохранение луж вместе с реагентами внутри.
/// TODO: Сохранение ржавых стен, битых лампочек
/// TODO: Блеклист реагентов в лужах(кровь, реагент 173) или сильное их сокращение, чтобы 173 не сбегал раундстартом
/// </summary>
public sealed class MetaGarbageSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;

    private static readonly HashSet<ProtoId<TagPrototype>> GarbageTags = ["Trash"];

    /// <summary>
    /// Сохраненный мусор, который будет передаваться из раунда в раунд.
    /// </summary>
    public readonly Dictionary<EntProtoId, List<StationMetaGarbageData>> CachedGarbage = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaGarbageTargetComponent, StationPostInitEvent>(OnMapInit);
        SubscribeLocalEvent<RealRoundEndedMessage>(OnRoundEnded);
    }

    private void OnMapInit(Entity<MetaGarbageTargetComponent> ent, ref StationPostInitEvent args)
    {
        var mapPrototype = Prototype(ent);

        if (mapPrototype == null)
            return;

        if (!CachedGarbage.TryGetValue(mapPrototype, out var list))
            return;

        var mapId = GetStationMapId(args.Station);
        var reducedGarbage = _helpers.GetPercentageOfHashSet(list, ent.Comp.SpawnPercent);

        foreach (var data in reducedGarbage)
        {
            var coords = new MapCoordinates(data.Position, mapId);
            var item = Spawn(data.Prototype, coords, rotation: data.Rotation);

            Logger.Debug($"Spawned {data.Prototype}|{item} at {data.Position} on map {mapId}|{Name(ent)}");
        }

        Logger.Debug($"Spawned {reducedGarbage.Count()}/{list.Count} item");
    }

    private void OnRoundEnded(RealRoundEndedMessage args)
    {
        var query = EntityQueryEnumerator<MetaGarbageTargetComponent, StationDataComponent>();

        while (query.MoveNext(out var uid, out _, out _))
        {
            var stationPrototype = Prototype(uid);

            if (stationPrototype == null)
                return;

            // Вычищаем прошлые данные о мусоре на данной карте и собираем их заново
            CachedGarbage.Remove(stationPrototype);
            CollectGarbage(uid, stationPrototype);
        }
    }

    private void CollectGarbage(EntityUid stationUid, EntProtoId stationPrototype)
    {
        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();

        var debugCount = 0;

        while (query.MoveNext(out var uid, out var tag, out var xform))
        {
            if (!IsValidEntityToSave(uid, tag))
                continue;

            var itemStation = _station.GetOwningStation(uid, xform);
            if (stationUid != itemStation)
                continue;

            var proto = Prototype(uid);
            if (proto == null)
                continue;

            // Сохраняем данные о мусоре в список для спавна в следующем раунде.
            var position = _transform.GetWorldPosition(xform);
            var rotation = _transform.GetWorldRotation(xform);

            // Добавляем в словарь данные.
            // Ключ - айди прототипа карты, чтобы разные карты имели разный набор мусора с прошлых смен
            // Значение - список мусора, который сохранен для данной карты.
            if (CachedGarbage.TryGetValue(stationPrototype, out var list))
                list.Add(new StationMetaGarbageData(proto, position, rotation));
            else
                CachedGarbage[stationPrototype] = [new StationMetaGarbageData(proto, position, rotation)];

            debugCount++;
        }

        Logger.Debug($"Saved {debugCount} trash items");
    }

    private bool IsValidEntityToSave(EntityUid uid, TagComponent tag)
    {
        if (!_tag.HasAnyTag(tag, GarbageTags))
            return false;

        if (HasComp<InsideEntityStorageComponent>(uid))
            return false;

        if (_container.IsEntityInContainer(uid))
            return false;

        return true;
    }

    private MapId GetStationMapId(Entity<StationDataComponent> ent)
    {
        var stationData = Comp<StationDataComponent>(ent);
        foreach (var grid in stationData.Grids)
        {
            var id = Transform(grid).MapID;

            if (id != MapId.Nullspace)
                return id;
        }

        // Сюда доходить не должно
        return Transform(ent).MapID;
    }
}
