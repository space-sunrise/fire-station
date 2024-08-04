using System.Numerics;
using Content.Server.Station.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Scp.Groups;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;


namespace Content.Server._Scp.Groups;

public sealed class MogSpawnSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("station.mogspawn");

        SubscribeLocalEvent<StationMogGroupsComponent, ComponentInit>(OnComponentInit);
    }

    public bool TrySpawnGroup(EntityUid station, string? targetGroup)
    {
        if (!TryComp<StationMogGroupsComponent>(station, out var stationMogGroupsComponent))
            return false;

        targetGroup ??= _random.Pick(stationMogGroupsComponent.AllowedGroups);

        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypeManager.TryIndex<MogGroupPrototype>(targetGroup, out var proto))
            return false;

        if (!TryComp(station, out StationDataComponent? stationDataComponent))
            return false;

        var grid = stationDataComponent.Grids.FirstOrNull();

        if (grid == null)
            return false;

        SpawnGroup(grid.Value, proto);

        return true;
    }

    private void SpawnGroup(EntityUid station, MogGroupPrototype targetGroup)
    {
        _sawmill.Debug($"spawngroup: {station}");

        var xform = Transform(station);
        var mapCoords = xform.Coordinates.ToMap(EntityManager);

        var targetCoords = mapCoords.Offset(SelectCoordinates(targetGroup));

        // Сделано именно так, чтобы выбирались зоны ближе и ближе к началу координат в поисках подходящей для высадки зоны.
        // По умолчанию станция находится на 0 0, так что в УЖАСНЕЙШЕМ случае они появятся прямо у входа в комплекс
        while (_lookup.AnyEntitiesInRange(targetCoords, 1f))
        {
            targetCoords.Offset(-Math.Sign(targetCoords.Y), -Math.Sign(targetCoords.Y));
        }

        var targetEntityCoords = _transform.ToCoordinates((station, xform), targetCoords);

        _sawmill.Debug($"spawngroup: {targetEntityCoords}");

        foreach (var entry in targetGroup.Group)
        {
            for (var i = 0; i < entry.Amount; i++)
            {
                _entMan.SpawnEntity(entry.Entity, targetEntityCoords);
            }
        }
    }

    private Vector2 SelectCoordinates(MogGroupPrototype targetGroup)
    {
        var angle = 2 * (float)Math.PI * _random.NextFloat();
        var distance = targetGroup.MinDistance +
                       (targetGroup.MaxDistance - targetGroup.MinDistance) * _random.NextFloat();

        var x = distance * (float)Math.Cos(angle);
        var y = distance * (float)Math.Sin(angle);

        return new Vector2(x, y);
    }

    private void OnComponentInit(EntityUid uid, StationMogGroupsComponent component, ComponentInit args)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        foreach (var group in component.AllowedGroups)
        {
            if (!prototypeManager.TryIndex<MogGroupPrototype>(group, out var proto))
            {
                _sawmill.Error($"No mog group found with ID {group}!");
                continue;
            }

            foreach (var entry in proto.Group)
            {
                _sawmill.Debug($"Found entry in group {group} with protoId: {entry.Entity} and amount: {entry.Amount}");
            }
        }
    }
}
