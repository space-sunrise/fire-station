using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.Ghost;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.GameRules.ScpSl;

public sealed partial class ScpSlGameRuleSystem
{
    private int _tracker = 0;

    //4014314031441404134044434414
    public static IReadOnlyList<int> SpawnQueue = [4, 0, 1, 4, 3, 1, 4, 0, 3, 1, 4, 4, 1, 4, 0, 4, 1, 3, 4, 0, 4, 4, 4, 3, 4, 4, 1, 4];

    private Dictionary<int, ScpSlHumanoidType> _numToHumanoidType = new()
    {
        { 1, ScpSlHumanoidType.Security },
        { 3, ScpSlHumanoidType.Scientist },
        { 4, ScpSlHumanoidType.ClassD },
    };

    private void InitializeSpawn()
    {
        SubscribeLocalEvent<SlScpSpawnPointComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(RulePlayerSpawningEvent ev)
    {
        if (!TryGetActiveRule(out var rule))
        {
            return;
        }

        InitialSpawn(rule.Value, ev.PlayerPool);
    }

    private void InitialSpawn(Entity<ScpSlGameRuleComponent> rule, List<ICommonSession> pool)
    {
        for (var i = 0; i < 5; i++)
        {
            _random.Shuffle(pool);
        }

        foreach (var player in pool)
        {
            ProcessSpawn(rule, player);
        }

        pool.Clear();
    }

    private void ProcessSpawn(Entity<ScpSlGameRuleComponent> rule, ICommonSession playerToSpawn)
    {
        if (_tracker >= SpawnQueue.Count)
        {
            _tracker = 0;
        }

        _gameTicker.PlayerJoinGame(playerToSpawn);

        var spawnType = SpawnQueue[_tracker];
        _tracker++;

        if (spawnType == 0 )
        {
            if (TrySpawnScp(rule, playerToSpawn))
            {
                return;
            }

            spawnType = 1;
        }

        if (!_numToHumanoidType.TryGetValue(spawnType, out var humanoidType))
        {
            humanoidType = ScpSlHumanoidType.ClassD;
        }

        SpawnHumanoid(rule, playerToSpawn, humanoidType);
    }

    public void SpawnHumanoid(Entity<ScpSlGameRuleComponent> rule, ICommonSession playerToSpawn, ScpSlHumanoidType humanoidType)
    {
        var spawnPosition = SelectRandomSpawnPosition(humanoidType);

        var humanoidPreset = SelectHumanoidPreset(rule, humanoidType);

        var humanoid = _randomHumanoidSystem.SpawnRandomHumanoid(humanoidPreset, spawnPosition, string.Empty);

        var mindEntity = _mindSystem.GetOrCreateMind(playerToSpawn.UserId);

        _mindSystem.TransferTo(mindEntity, humanoid);
    }

    public bool TrySpawnScp(Entity<ScpSlGameRuleComponent> rule, ICommonSession playerToSpawn)
    {
        var scpSpawns = GetScpSpawns();

        if (scpSpawns.Count == 0)
        {
            return false;
        }

        var spawn = _random.PickAndTake(scpSpawns);
        var spawnPosition = spawn.Comp1.Coordinates;
        var scpProto = spawn.Comp2.ScpProtoId;

        var scpEntity = Spawn(scpProto, spawnPosition);
        AddComp<ScpSlScpMarkerComponent>(scpEntity);

        var mindEntity = _mindSystem.GetOrCreateMind(playerToSpawn.UserId);

        _mindSystem.TransferTo(mindEntity, scpEntity);

        Del(spawn);
        return true;
    }

    private void SpawnUpdate(Entity<ScpSlGameRuleComponent> rule)
    {
        if (rule.Comp.NextWaveSpawnTime > _gameTiming.CurTime)
        {
            return;
        }

        rule.Comp.NextWaveSpawnTime = _gameTiming.CurTime + rule.Comp.WaveSpawnCooldown;

        SpawnWave(rule);
    }

    private void OnMapInit(Entity<SlScpSpawnPointComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (ent.Comp.Playable)
        {
            return;
        }

        Spawn(ent.Comp.ScpProtoId, xform.Coordinates);

        QueueDel(ent);
    }

    private void SpawnChaosWave(Entity<ScpSlGameRuleComponent> rule, List<ICommonSession> pool)
    {
        if (pool.Count == 0)
        {
            return;
        }

        var playersToSpawn = Math.Min(pool.Count, _maxChaosSpawnCount);

        for (int i = 0; i < playersToSpawn; i++)
        {
            var player = _random.PickAndTake(pool);

            SpawnHumanoid(rule, player, ScpSlHumanoidType.Chaos);
        }
    }

    private void SpawnWave(Entity<ScpSlGameRuleComponent> rule)
    {
        var players = _playerManager.NetworkedSessions;

        var eligible = GetEligiblePlayersForWave(players.ToList());

        if (eligible.Count == 0)
        {
            return;
        }

        if (_random.Prob(_chaosSpawnChance))
        {
            SpawnChaosWave(rule, eligible);
            return;
        }

        SpawnMogWave(rule, eligible);
    }

    private void SpawnMogWave(Entity<ScpSlGameRuleComponent> rule, List<ICommonSession> pool)
    {
        if (pool.Count == 0)
        {
            return;
        }

        var spawnPoint = SelectRandomSpawnPosition(ScpSlHumanoidType.Mog);

        var captainPlayer = _random.PickAndTake(pool);
        var captainEntity = _randomHumanoidSystem.SpawnRandomHumanoid(rule.Comp.MogCaptainPrototype, spawnPoint, string.Empty);

        var captainMind = _mindSystem.GetOrCreateMind(captainPlayer.UserId);
        _mindSystem.TransferTo(captainMind, captainEntity);

        var playersToSpawn = Math.Min(pool.Count, _maxMogSpawnCount);


        for (var i = 0; i < playersToSpawn; i++)
        {
            var player = _random.PickAndTake(pool);

            SpawnHumanoid(rule, player, ScpSlHumanoidType.Mog);
        }
    }

    private List<ICommonSession> GetEligiblePlayersForWave(List<ICommonSession> sessions)
    {
        var eligiblePlayers = new List<ICommonSession>();

        foreach (var session in sessions)
        {
            var playerEntity = session.AttachedEntity;

            if (!playerEntity.HasValue || !HasComp<GhostComponent>(playerEntity))
            {
                continue;
            }

            eligiblePlayers.Add(session);
        }

        return eligiblePlayers;
    }

    private ProtoId<RandomHumanoidSettingsPrototype> SelectHumanoidPreset(Entity<ScpSlGameRuleComponent> rule, ScpSlHumanoidType humanoidType)
    {
        var outfits = rule.Comp.HumanoidPresets[humanoidType];
        return _random.Pick(outfits);
    }

    private EntityCoordinates SelectRandomSpawnPosition(ScpSlHumanoidType humanoidType)
    {
        var spawners = GetHumanoidSpawnPoints(humanoidType);
        return _random.Pick(spawners).Comp1.Coordinates;
    }

    private List<Entity<TransformComponent, SlHumanoidSpawnPointComponent>> GetHumanoidSpawnPoints(ScpSlHumanoidType spawnPointType)
    {
        var spawnPointsQuery = EntityQueryEnumerator<TransformComponent, SlHumanoidSpawnPointComponent>();

        List<Entity<TransformComponent, SlHumanoidSpawnPointComponent>> spawnPoints = [];

        while (spawnPointsQuery.MoveNext(out var entityUid, out var transformComponent, out var spawnPointComponent))
        {
            var entity = new Entity<TransformComponent, SlHumanoidSpawnPointComponent>(entityUid, transformComponent, spawnPointComponent);
            spawnPoints.Add(entity);
        }

        return spawnPoints;
    }

    private List<Entity<TransformComponent, SlScpSpawnPointComponent>> GetScpSpawns()
    {
        var spawnPointsQuery = EntityQueryEnumerator<TransformComponent, SlScpSpawnPointComponent>();

        List<Entity<TransformComponent, SlScpSpawnPointComponent>> spawnPoints = [];

        while (spawnPointsQuery.MoveNext(out var entityUid, out var transformComponent, out var spawnPointComponent))
        {
            var entity = new Entity<TransformComponent, SlScpSpawnPointComponent>(entityUid, transformComponent, spawnPointComponent);
            spawnPoints.Add(entity);
        }

        return spawnPoints;
    }
}
