using System.Linq;
using Content.Server._Sunrise.Helpers;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared._Scp.Other.AirlockManEater;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.GameTicking.Rules.AirlockManEater;

public sealed class AirlockManEaterRule : StationEventSystem<AirlockManEaterRuleComponent>
{
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, AirlockManEaterRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        Jailbreak(chosenStation.Value, component.Percentage);
    }

    private void Jailbreak(EntityUid station, float percentage)
    {
        var airlocks = _helpers.GetAll<AirlockComponent>()
            .Where(e => _station.GetOwningStation(e) == station)
            .ToList();

        _random.Shuffle(airlocks);

        var targetAirlocks = _helpers.GetPercentageOfHashSet(airlocks, percentage);

        foreach (var airlock in targetAirlocks)
        {
            AddComp<AirlockManEaterComponent>(airlock);
        }
    }
}
