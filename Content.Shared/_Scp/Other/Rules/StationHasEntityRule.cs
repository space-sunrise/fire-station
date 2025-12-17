using Content.Shared.Random.Rules;
using Content.Shared.Station.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Shared._Scp.Other.Rules;

public sealed partial class StationHasEntityRule : RulesRule
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    private EntityLookupSystem? _lookup;
    private EntityWhitelistSystem? _whitelist;

    private EntityQuery<TransformComponent> _xformQuery;

    private readonly HashSet<Entity<TransformComponent>> _entities = [];

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        _xformQuery = entManager.GetEntityQuery<TransformComponent>();

        var mapId = GetStationMapId(uid, entManager);
        if (mapId == MapId.Nullspace)
            return Inverted;

        _lookup ??= entManager.System<EntityLookupSystem>();
        _whitelist ??= entManager.System<EntityWhitelistSystem>();

        _entities.Clear();
        _lookup.GetEntitiesOnMap(mapId, _entities);

        foreach (var ent in _entities)
        {
            if (_whitelist.CheckBoth(ent, Blacklist, Whitelist))
                return !Inverted;
        }

        return Inverted;
    }

    private MapId GetStationMapId(EntityUid station, EntityManager ent)
    {
        if (!ent.TryGetComponent<StationDataComponent>(station, out var stationData))
            return MapId.Nullspace;

        foreach (var gridUid in stationData.Grids)
        {
            if (!_xformQuery.TryGetComponent(gridUid, out var xform))
                continue;

            return xform.MapID;
        }

        return MapId.Nullspace;
    }
}
