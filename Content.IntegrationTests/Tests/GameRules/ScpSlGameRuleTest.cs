using System.Linq;
using Content.Server._Scp.GameRules.ScpSl;
using Content.Server._Sunrise.StationCentComm;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.GameTicking;
using Content.Shared.Pinpointer;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class ScpSlGameRuleTest
{
    [Test]
    public async Task TryStartGameRuleTestWithSufficientPlayers()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var client = pair.Client;

        var entMan = server.EntMan;

        var ticker = server.System<GameTicker>();
        var xformSystem = server.System<TransformSystem>();
        var physicsSystem = server.System<PhysicsSystem>();

        Assert.That(ticker.DummyTicker, Is.False);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        var dummies = await pair.Server.AddDummySessions(30);

        await pair.RunTicksSync(5);

        // Initially, the players have no attached entities
        Assert.That(pair.Player?.AttachedEntity, Is.Null);
        Assert.That(dummies.All(x => x.AttachedEntity == null));

        Assert.That(entMan.Count<MapComponent>(), Is.Zero);
        Assert.That(entMan.Count<MapGridComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationMapComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationMemberComponent>(), Is.Zero);
        Assert.That(entMan.Count<StationCentCommComponent>(), Is.Zero); // Sunrise-Edit

        // And no sl related components
        Assert.That(entMan.Count<ScpSlGameRuleComponent>(), Is.Zero);
        Assert.That(entMan.Count<ScpSlHumanoidMarkerComponent>(), Is.Zero);
        Assert.That(entMan.Count<ScpSlScpMarkerComponent>(), Is.Zero);

        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.ReadyToPlay));

        await pair.WaitCommand("forcepreset ScpSl");
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
        Assert.That(client.EntMan.EntityExists(client.AttachedEntity));

        Assert.That(entMan.Count<ScpSlGameRuleComponent>(), Is.EqualTo(1));
        Assert.That(entMan.Count<ScpSlHumanoidMarkerComponent>, Is.AtLeast(1));
        Assert.That(entMan.Count<ScpSlScpMarkerComponent>, Is.AtLeast(1));


        //Try escape

        var (escapeZoneXform, _) = entMan.EntityQuery<TransformComponent, ScpSlEscapeZoneComponent>().First();

        var chaosBefore = dummies.Count(x =>
            entMan.TryGetComponent<ScpSlHumanoidMarkerComponent>(x.AttachedEntity!.Value!, out var marker) &&
            marker.HumanoidType == ScpSlHumanoidType.Chaos);

        var dBefore = dummies.Count(x =>
            entMan.TryGetComponent<ScpSlHumanoidMarkerComponent>(x.AttachedEntity!.Value!, out var marker) &&
            marker.HumanoidType == ScpSlHumanoidType.ClassD);

        var dummyD = dummies
            .First(x => entMan.TryGetComponent<ScpSlHumanoidMarkerComponent>(x.AttachedEntity!.Value!, out var marker) &&
                        marker.HumanoidType == ScpSlHumanoidType.ClassD)
            .AttachedEntity!.Value!;

        physicsSystem.WakeBody(escapeZoneXform.Owner);
        physicsSystem.WakeBody(dummyD);

        await pair.RunTicksSync(5);

        xformSystem.SetCoordinates(dummyD, escapeZoneXform.Coordinates);

        await pair.RunTicksSync(5);

        var dAfter = dummies.Count(x =>
            entMan.TryGetComponent<ScpSlHumanoidMarkerComponent>(x.AttachedEntity!.Value!, out var marker) &&
            marker.HumanoidType == ScpSlHumanoidType.ClassD);

        var chaosAfter = dummies.Count(x =>
            entMan.TryGetComponent<ScpSlHumanoidMarkerComponent>(x.AttachedEntity!.Value!, out var marker) &&
            marker.HumanoidType == ScpSlHumanoidType.Chaos);


        Assert.That(dBefore > dAfter, Is.True);
        Assert.That(chaosBefore < chaosAfter, Is.True);

        // Try end round
        var player = pair.Player!.AttachedEntity!.Value;

        var dummyEntity = dummies.Select(x => x.AttachedEntity ?? default).Append(player).Where(x=> entMan.HasComponent<ScpSlHumanoidMarkerComponent>(x)).ToList();

        await server.WaitAssertion(() =>
        {
            foreach (var entityUid in dummyEntity)
            {
                entMan.DeleteEntity(entityUid);
            }

        });

        Assert.That(ticker.RunLevel == GameRunLevel.PostRound, Is.True, "All humanoids are dead, but round doesn't end!");

        ticker.SetGamePreset((GamePresetPrototype?) null);
        await pair.CleanReturnAsync();
    }
}
