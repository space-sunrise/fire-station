using System.Diagnostics.CodeAnalysis;
using Content.Server.Cuffs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Nuke;
using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Scp.GameRules.ScpSl;

public sealed partial class ScpSlGameRuleSystem : GameRuleSystem<ScpSlGameRuleComponent>
{
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly CuffableSystem _cuffableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoidSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;

    protected override string SawmillName => "ScpSl";

    private TimeSpan _nextRoundEndCheckTime;
    private EntProtoId _ashPrototype = "Ash";

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(LoadMapRuleSystem));

        InitializeEscape();
        InitializeSpawn();
        InitializeSubs();
        InitializeTracker();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        if (!TryGetActiveRule(out var gameRule)
            || !ev.OwningStation.HasValue)
        {
            return;
        }

        var stationUid = ev.OwningStation.Value;
        var zones = gameRule.Value.Comp1.Zones.Values;

        if (zones.FirstOrNull(x => x.Owner == stationUid) == null)
        {
            return;
        }

        foreach (var zone in zones)
        {
            DustifyAllMobsOnGrid(zone);
        }
    }

    private List<Entity<MobStateComponent>> GetAllMobstatesOnGrid(Entity<MapGridComponent> gridEntity)
    {
        var query = EntityQueryEnumerator<TransformComponent, MobStateComponent>();
        var mobs = new List<Entity<MobStateComponent>>();

        while (query.MoveNext(out var uid, out var xform, out var mobStateComponent))
        {
            if (xform.GridUid != gridEntity)
                continue;

            var mobEntity = new Entity<MobStateComponent>(uid, mobStateComponent);
            mobs.Add(mobEntity);
        }

        return mobs;
    }

    private void DustifyAllMobsOnGrid(Entity<MapGridComponent> gridEntity)
    {
        var mobs = GetAllMobstatesOnGrid(gridEntity);

        foreach (var mob in mobs)
        {
            var mobXform = Transform(mob);
            _mobStateSystem.ChangeMobState(mob, MobState.Dead);

            Spawn(_ashPrototype, mobXform.Coordinates);

            Del(mob);
        }
    }

    protected override void ActiveTick(EntityUid uid, ScpSlGameRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var ruleEntity = new Entity<ScpSlGameRuleComponent>(uid, component);
        SpawnUpdate(ruleEntity);

        if (_nextRoundEndCheckTime <  _gameTiming.CurTime)
        {
            _nextRoundEndCheckTime = _gameTiming.CurTime + TimeSpan.FromSeconds(5);

            TryEndRound();
        }
    }

    protected override void Started(EntityUid uid, ScpSlGameRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextWaveSpawnTime = _gameTiming.CurTime + component.WaveSpawnCooldown;
        _nextRoundEndCheckTime = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ScpSlGameRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
    }

    private void TryEndRound()
    {
        if (!TryGetActiveRule(out var rule))
        {
            return;
        }

        var slGameRuleComponent = rule.Value.Comp1;

        var dAlive = GetAliveHumanoidsCount(ScpSlHumanoidType.ClassD);
        var mogAlive = GetAliveHumanoidsCount(ScpSlHumanoidType.Mog) + GetAliveHumanoidsCount(ScpSlHumanoidType.Scientist);
        var chaosAlive = GetAliveHumanoidsCount(ScpSlHumanoidType.Chaos);
        var scpAlive = GetAliveScpCount();


        var shouldRoundEnd = false;

        if (mogAlive > 0 && dAlive == 0 && scpAlive == 0)
        {
            shouldRoundEnd = true;

            if (slGameRuleComponent.EscapedScientists > slGameRuleComponent.EscapedDClass)
            {
                slGameRuleComponent.WinType = SlWinType.FondWin;
            }
            else
            {
                slGameRuleComponent.WinType = SlWinType.Tie;
            }
        }

        if (scpAlive > 0 && dAlive == 0 && mogAlive == 0)
        {
            shouldRoundEnd = true;

            if (scpAlive > slGameRuleComponent.EscapedDClass + slGameRuleComponent.EscapedScientists)
            {
                slGameRuleComponent.WinType = SlWinType.ScpWin;
            }
            else if (slGameRuleComponent.EscapedDClass > slGameRuleComponent.EscapedScientists + scpAlive)
            {
                slGameRuleComponent.WinType = SlWinType.ChaosWin;
            }
            else
            {
                slGameRuleComponent.WinType = SlWinType.Tie;
            }
        }

        if ((dAlive > 0 || chaosAlive > 0) && scpAlive == 0 && mogAlive == 0)
        {
            shouldRoundEnd = true;

            if (slGameRuleComponent.EscapedScientists < slGameRuleComponent.EscapedDClass)
            {
                slGameRuleComponent.WinType = SlWinType.ChaosWin;
            }
            else
            {
                slGameRuleComponent.WinType = SlWinType.Tie;
            }
        }

        if (dAlive == 0 && mogAlive == 0 && scpAlive == 0)
        {
            shouldRoundEnd = true;
            slGameRuleComponent.WinType = SlWinType.Tie;
        }

        if (shouldRoundEnd)
        {
            _gameTicker.EndRound();
        }
    }

    private bool TryGetActiveRule([NotNullWhen(true)] out Entity<ScpSlGameRuleComponent, GameRuleComponent>? gameRule)
    {
        gameRule = null;

        var query = QueryActiveRules();

        while (query.MoveNext(out var _, out var scpRuleComponent, out var ruleComponent))
        {
            gameRule = new Entity<ScpSlGameRuleComponent, GameRuleComponent>(scpRuleComponent.Owner, scpRuleComponent, ruleComponent);
        }

        return gameRule.HasValue;
    }

    private int GetAliveHumanoidsCount(ScpSlHumanoidType humanoidType)
    {
        var query = EntityQueryEnumerator<ScpSlHumanoidMarkerComponent, MobStateComponent>();

        var alive = 0;
        while (query.MoveNext(out var _, out var markerComponent, out var mobStateComponent))
        {
            if (mobStateComponent.CurrentState is MobState.Alive or MobState.Critical
                && markerComponent.HumanoidType == humanoidType)
            {
                alive++;
            }
        }

        return alive;
    }

    private int GetAliveScpCount()
    {
        var query = EntityQueryEnumerator<ScpSlScpMarkerComponent, MobStateComponent>();
        var alive = 0;

        while (query.MoveNext(out var _, out var marker, out var _))
        {
            if (!marker.Contained)
            {
                alive++;
            }
        }

        return alive;
    }
}
