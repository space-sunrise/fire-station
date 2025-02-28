using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.Helpers;

/// <summary>
/// Система-набор хелпер методов
/// </summary>
public sealed class ScpHelpersSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    #region Private

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

    #endregion

    public bool TryFindRandomTile(out Vector2i tile,
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

    public bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
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
                || _atmosphere.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp)
                || !_map.TryGetTileRef(targetGrid, gridComp, tile, out var tileRef)
                || tileRef.Tile.IsEmpty)
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            break;
        }

        return found;
    }


    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    /// <typeparam name="T">Компонент</typeparam>
    [Obsolete]
    public IEnumerable<T> GetPercentageOfHashSet<T>(IReadOnlyCollection<T> sourceList, float percentage) where T : IComponent
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(t => !Transform(t.Owner).Anchored).Take(countToAdd).ToHashSet();
    }

    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    /// <typeparam name="T">Ентити с компонентом</typeparam>
    public IEnumerable<Entity<T>> GetPercentageOfHashSet<T>(IReadOnlyCollection<Entity<T>> sourceList, float percentage) where T : IComponent
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(e => !Transform(e).Anchored).Take(countToAdd).ToHashSet();
    }

    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    public IEnumerable<EntityUid> GetPercentageOfHashSet(IReadOnlyCollection<EntityUid> sourceList, float percentage)
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(e => !Transform(e).Anchored).Take(countToAdd).ToHashSet();
    }
}
