using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Systems;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106System : SharedScp106System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StairsSystem _stairs = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<Scp106Component> ent, ref MapInitEvent args)
    {
        var marks = SearchForMarks();
        if (marks.Count == 0)
            _ = _stairs.GenerateFloor();
    }

    public override async void SendToBackrooms(EntityUid target)
    {
        // You already here.
        if (HasComp<Scp106BackRoomMapComponent>(Transform(target).MapUid))
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        var mark = await GetTransferMark();
        _transform.SetCoordinates(target, mark);
        _transform.AttachToGridOrMap(target);
    }

    public override void SendToStation(EntityUid target)
    {
        if (!TryFindRandomTile(out _, out _, out _, out var targetCoords))
            return;

        _transform.SetCoordinates(target, targetCoords);
        _transform.AttachToGridOrMap(target);
    }

    private async Task<EntityCoordinates> GetTransferMark()
    {
        var marks = SearchForMarks();
        if (marks.Count != 0)
            return _random.Pick(marks);

        // Impossible, but just to be sure.
        await _stairs.GenerateFloor();
        return _random.Pick(SearchForMarks());
    }

    private HashSet<EntityCoordinates> SearchForMarks()
    {
        return EntityQuery<Scp106BackRoomMarkComponent>()
            .Select(entity => Transform(entity.Owner).Coordinates)
            .ToHashSet();
    }

    private bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        station = stations[_random.Next(stations.Count)];
        return true;
    }

    private bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    private bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        var weights = new Dictionary<Entity<MapGridComponent>, float>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;

            weights.Add((possibleTarget, comp), _map.GetAllTiles(possibleTarget, comp).Count());
        }

        if (weights.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }

        (targetGrid, var gridComp) = _random.Pick(weights);

        var found = false;
        var aabb = gridComp.LocalAABB;

        for (var i = 0; i < 10; i++)
        {
            var randomX = _random.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = _random.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile)
                || _atmosphere.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            break;
        }

        return found;
    }

}
