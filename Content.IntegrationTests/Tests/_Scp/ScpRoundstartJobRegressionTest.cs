#nullable enable
using System.Collections.Generic;
using System.Reflection;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Scp;

/// <summary>
/// Regression suite for roundstart job assignment.
/// Protects against bugs where players stay in the lobby after pressing Ready
/// due to incorrect role filtering and/or incorrect reassignment of an already assigned job.
/// </summary>
[TestFixture]
public sealed class ScpRoundstartJobRegressionTest
{
    private const string RoleTimerAssistant = "TRoleTimerAssistant";
    private const string RoleTimerEngineer = "TRoleTimerEngineer";
    private const string ScpTestAssistant = "ScpTestAssistant";
    private const string ScpTestCaptain = "ScpTestCaptain";

    private const string RoleTimerMapId = "ScpJobRoleTimerTestMap";
    private const string StationMapNoDockId = "ScpJobStationNoDockMap";

    [TestPrototypes]
    private static readonly string Prototypes = $@"
- type: playTimeTracker
  id: ScpPlayTimeRoleTimerAssistant

- type: playTimeTracker
  id: ScpPlayTimeRoleTimerEngineer

- type: playTimeTracker
  id: ScpPlayTimeAssistant

- type: playTimeTracker
  id: ScpPlayTimeCaptain

- type: gameMap
  id: {RoleTimerMapId}
  mapName: {RoleTimerMapId}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            {RoleTimerAssistant}: [ 1, 1 ]
            {RoleTimerEngineer}: [ 1, 1 ]

- type: gameMap
  id: {StationMapNoDockId}
  minPlayers: 0
  mapName: {StationMapNoDockId}
  mapPath: /Maps/Test/empty.yml
  stations:
    Station:
      mapNameTemplate: {StationMapNoDockId}
      stationProto: StandardStationArena
      components:
        - type: StationJobs
          availableJobs:
            {ScpTestAssistant}: [ -1, -1 ]
            {ScpTestCaptain}: [ 5, 5 ]

- type: job
  id: {RoleTimerAssistant}
  playTimeTracker: ScpPlayTimeRoleTimerAssistant

- type: job
  id: {RoleTimerEngineer}
  playTimeTracker: ScpPlayTimeRoleTimerEngineer

- type: job
  id: {ScpTestAssistant}
  playTimeTracker: ScpPlayTimeAssistant

- type: job
  id: {ScpTestCaptain}
  weight: 10
  playTimeTracker: ScpPlayTimeCaptain
";

    /// <summary>
    /// Verifies that a player has actually spawned into the round and received the expected job.
    /// </summary>
    private static void AssertJob(TestPair pair, ProtoId<JobPrototype> job, NetUserId? user = null, bool isAntag = false)
    {
        var jobSys = pair.Server.System<SharedJobSystem>();
        var mindSys = pair.Server.System<MindSystem>();
        var roleSys = pair.Server.System<RoleSystem>();
        var ticker = pair.Server.System<GameTicker>();

        user ??= pair.Client.User!.Value;

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[user.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));

        var uid = pair.Server.PlayerMan.SessionsDict.GetValueOrDefault(user.Value)?.AttachedEntity;
        Assert.That(pair.Server.EntMan.EntityExists(uid));
        var mind = mindSys.GetMind(uid!.Value);
        Assert.That(pair.Server.EntMan.EntityExists(mind));
        Assert.That(jobSys.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));
        Assert.That(roleSys.MindIsAntagonist(mind), Is.EqualTo(isAntag));
    }

    /// <summary>
    /// Verifies that with role timers enabled, a player with valid priorities
    /// does not remain in the lobby on roundstart when a real slot is available.
    /// This guards against regressions that incorrectly mark roles as disallowed
    /// and produce mass "no job available" outcomes.
    /// </summary>
    [Test]
    public async Task RoundstartRoleTimersEnabledNoOverflowPlayerStillSpawnsTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
            Fresh = true,
        });

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, RoleTimerMapId);
        pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, true);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.SetJobPriorities((RoleTimerAssistant, JobPriority.Medium), (RoleTimerEngineer, JobPriority.High));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());

        AssertJob(pair, RoleTimerEngineer);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that a job already assigned for roundstart is preserved
    /// when it is still available (slot open and role not restricted).
    /// This protects against regressions from unconditional repick behavior
    /// that breaks valid assignments.
    /// </summary>
    [Test]
    public async Task RoundstartAssignedJobRetainedWhenStillAvailableTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
            Fresh = true,
        });

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, RoleTimerMapId);
        pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, false);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.SetJobPriorities((RoleTimerAssistant, JobPriority.Low), (RoleTimerEngineer, JobPriority.High));

        await pair.Server.WaitPost(() => ticker.StartRound(true));

        await pair.Server.WaitPost(() =>
        {
            var stationQuery = pair.Server.EntMan.EntityQueryEnumerator<StationJobsComponent>();
            Assert.That(stationQuery.MoveNext(out var station, out _), Is.True);

            var session = pair.Server.PlayerMan.SessionsDict[pair.Client.User!.Value];
            var spawnPlayer = typeof(GameTicker).GetMethod(
                "SpawnPlayer",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(ICommonSession), typeof(EntityUid), typeof(string), typeof(bool), typeof(bool), typeof(bool)],
                null);

            Assert.That(spawnPlayer, Is.Not.Null);
            spawnPlayer!.Invoke(ticker, [session, station, RoleTimerAssistant, false, true, true]);
        });

        AssertJob(pair, RoleTimerAssistant);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies correct role selection when <c>disallowedJobs = null</c>.
    /// Protects against a nullable bug in <c>PickBestAvailableJobWithPriority</c>
    /// that could otherwise return no job while valid roles are available.
    /// </summary>
    [Test]
    public async Task PickBestAvailableJobWithPriorityNullDisallowedWorks()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Fresh = true,
        });
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();
        var stationProto = prototypeManager.Index<GameMapPrototype>(StationMapNoDockId);

        var station = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            station = stationSystem.InitializeNewStation(stationProto.Stations["Station"], null, "Scp Test Station");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                [ScpTestCaptain] = JobPriority.High,
                [ScpTestAssistant] = JobPriority.Medium,
            };

            var picked = stationJobs.PickBestAvailableJobWithPriority(station, priorities, false);
            Assert.That(picked, Is.EqualTo(ScpTestCaptain));
        });

        await pair.CleanReturnAsync();
    }
}
