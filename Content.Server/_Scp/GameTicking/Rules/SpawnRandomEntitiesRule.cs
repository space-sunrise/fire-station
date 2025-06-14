﻿using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server._Sunrise.Helpers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class SpawnRandomEntitiesRule : StationEventSystem<SpawnRandomEntitiesRuleComponent>
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;

    protected override void Started(EntityUid uid, SpawnRandomEntitiesRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        if (!TryComp<StationDataComponent>(station, out var stationDataComponent))
            return;

        var totalTiles = _station.GetTileCount(stationDataComponent);

        var dirtyMod = RobustRandom.NextGaussian(component.TilesPerEntityAverage, component.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            if (!_helpers.TryFindRandomTileOnStation((station.Value, stationDataComponent), out _, out _, out var coords))
                continue;

            var ents = EntitySpawnCollection.GetSpawns(component.Entities, RobustRandom);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }
}
