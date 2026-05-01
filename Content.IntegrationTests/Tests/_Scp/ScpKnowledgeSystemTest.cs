#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Server._Scp.Knowledge;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared._Scp.Knowledge;
using Content.Shared._Scp.Knowledge.Components;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Scp;

[TestFixture]
public sealed class ScpKnowledgeSystemTest
{
    private const string PlayerPrototype = "MobHuman";
    private const string ClassDJob = "ClassD";

    private static readonly ProtoId<ScpKnowledgePrototype> KnowledgeContainmentBreach = "ScpKnowledgeTermContainmentBreach";
    private static readonly ProtoId<ScpKnowledgePrototype> KnowledgeClassDZone = "ScpKnowledgeLocationClassDContainmentZone";
    private static readonly ProtoId<ScpKnowledgePrototype> KnowledgeHeavyContainment = "ScpKnowledgeTermHeavyContainmentZone";
    private static readonly ProtoId<ScpKnowledgePrototype> KnowledgeScp131 = "ScpKnowledgeEntityScp131";
    private static readonly ProtoId<ScpKnowledgePrototype> KnowledgeScp173 = "ScpKnowledgeEntityScp173";

    [Test]
    public async Task SpeechDoesNotMatchArbitraryGapsInScpDesignation()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();
        EntityUid speaker = default;

        await pair.Server.WaitPost(() =>
        {
            var coordinates = pair.Server.EntMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            speaker = pair.Server.EntMan.SpawnEntity(PlayerPrototype, coordinates);
            Assert.That(knowledge.TryGrantKnowledgeProgress(speaker, KnowledgeScp173, 2), Is.True);

            var chat = pair.Server.System<ChatSystem>();
            chat.TrySendInGameICMessage(
                speaker,
                "scp many random words 173",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out _), Is.False);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task HumanKnowledgeIsConfiguredOnBodyViaJobSpecial()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(pair.Server.EntMan.TryGetComponent(setup.Body, out ScpKnowledgeComponent? _), Is.True);
                Assert.That(pair.Server.EntMan.TryGetComponent(setup.Body, out ScpKnowledgeAcquisitionComponent? _), Is.True);
                Assert.That(pair.Server.EntMan.TryGetComponent(setup.Mind, out ScpKnowledgeComponent? _), Is.False);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeContainmentBreach), Is.False);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeClassDZone), Is.False);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeHeavyContainment), Is.False);
            });
        });

        await pair.Server.WaitPost(() => ApplyJobToBody(pair, setup.Mind, setup.Body, ClassDJob));
        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeContainmentBreach), Is.True);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeClassDZone), Is.True);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeHeavyContainment), Is.False);
            });
        });

        EntityUid secondBody = default;
        await pair.Server.WaitPost(() =>
        {
            var entMan = pair.Server.EntMan;
            var mindSystem = pair.Server.System<MindSystem>();
            var coordinates = entMan.GetComponent<TransformComponent>(setup.Body).Coordinates;

            secondBody = entMan.SpawnEntity(PlayerPrototype, coordinates);
            mindSystem.TransferTo(setup.Mind, secondBody);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(pair.Server.EntMan.TryGetComponent(secondBody, out ScpKnowledgeComponent? _), Is.True);
                Assert.That(knowledge.HasKnowledge(secondBody, KnowledgeClassDZone), Is.False);
                Assert.That(knowledge.HasKnowledge(secondBody, KnowledgeContainmentBreach), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LocalSpeechAndRadioRequireExamineToUnlockScpKnowledge()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();
        EntityUid speaker = default;
        EntityUid scp173 = default;

        await pair.Server.WaitPost(() =>
        {
            var coordinates = pair.Server.EntMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            speaker = pair.Server.EntMan.SpawnEntity(PlayerPrototype, coordinates);

            var chat = pair.Server.System<ChatSystem>();
            chat.TrySendInGameICMessage(
                speaker,
                "объекту 173",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out _), Is.False);
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
            });
        });

        await pair.Server.WaitPost(() =>
        {
            Assert.That(knowledge.TryGrantKnowledgeProgress(speaker, KnowledgeScp173, 2), Is.True);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitPost(() =>
        {
            var chat = pair.Server.System<ChatSystem>();
            chat.TrySendInGameICMessage(
                speaker,
                "SCP-173",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out var progress), Is.True);
                Assert.That(progress, Is.EqualTo(1));
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
            });
        });

        await pair.Server.WaitPost(() =>
        {
            var chat = pair.Server.System<ChatSystem>();
            chat.TrySendInGameICMessage(
                speaker,
                "SCP-173",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out var progress), Is.True);
                Assert.That(progress, Is.EqualTo(1));
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
            });
        });

        await pair.Server.WaitPost(() =>
        {
            var coordinates = pair.Server.EntMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            scp173 = pair.Server.EntMan.SpawnEntity("Scp173", coordinates);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                speaker,
                new RadioSpokeEvent(speaker, "Костоломка", [setup.Body]),
                true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out var progress), Is.True);
                Assert.That(progress, Is.EqualTo(1));
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
            });
        });

        await pair.Server.WaitPost(() =>
        {
            Assert.That(knowledge.TryGrantExamineKnowledge(setup.Body, scp173), Is.True);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out var progress), Is.True);
                Assert.That(progress, Is.EqualTo(2));
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PaperReadRequiresKnowledgeableAuthorOrCodeAndDoesNotFarmTheSameSource()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();
        EntityUid ignorantWriter = default;
        EntityUid knowledgeableWriter = default;
        EntityUid scp131 = default;

        EntityUid ignorantPaper = default;
        EntityUid knowledgeablePaper = default;
        EntityUid codePaper = default;
        await pair.Server.WaitPost(() =>
        {
            var entMan = pair.Server.EntMan;
            var coordinates = entMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            ignorantWriter = entMan.SpawnEntity(PlayerPrototype, coordinates);
            knowledgeableWriter = entMan.SpawnEntity(PlayerPrototype, coordinates);
            ignorantPaper = entMan.SpawnEntity("Paper", coordinates);
            knowledgeablePaper = entMan.SpawnEntity("Paper", coordinates);
            codePaper = entMan.SpawnEntity("Paper", coordinates);
            scp131 = entMan.SpawnEntity("Scp131A", coordinates);

            Assert.That(knowledge.TryGrantKnowledgeProgress(knowledgeableWriter, KnowledgeScp131, 2), Is.True);

            var paperSystem = pair.Server.System<PaperSystem>();
            paperSystem.SetContent(ignorantPaper, "scp 131", ignorantWriter);
            paperSystem.SetContent(knowledgeablePaper, "scp 131", knowledgeableWriter);
            paperSystem.SetContent(codePaper, "scp 131");
        });

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                ignorantPaper,
                new BeforeActivatableUIOpenEvent(setup.Body),
                true);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out _), Is.False);
        });

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                knowledgeablePaper,
                new BeforeActivatableUIOpenEvent(setup.Body),
                true);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out var progress), Is.True);
            Assert.That(progress, Is.EqualTo(1));
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp131), Is.False);
        });

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                knowledgeablePaper,
                new BeforeActivatableUIOpenEvent(setup.Body),
                true);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out var progress), Is.True);
            Assert.That(progress, Is.EqualTo(1));
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp131), Is.False);
        });

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                codePaper,
                new BeforeActivatableUIOpenEvent(setup.Body),
                true);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out var progress), Is.True);
            Assert.That(progress, Is.EqualTo(1));
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp131), Is.False);
        });

        await pair.Server.WaitPost(() =>
        {
            Assert.That(knowledge.TryGrantExamineKnowledge(setup.Body, scp131), Is.True);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out var progress), Is.True);
            Assert.That(progress, Is.EqualTo(2));
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp131), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PaperOnlyAttributesLiteralChangesToKnowledgeableWriter()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();
        EntityUid paper = default;
        EntityUid knowledgeableWriter = default;

        await pair.Server.WaitPost(() =>
        {
            var entMan = pair.Server.EntMan;
            var coordinates = entMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            paper = entMan.SpawnEntity("Paper", coordinates);
            knowledgeableWriter = entMan.SpawnEntity(PlayerPrototype, coordinates);

            Assert.That(knowledge.TryGrantKnowledgeProgress(knowledgeableWriter, KnowledgeScp131, 2), Is.True);

            var paperSystem = pair.Server.System<PaperSystem>();
            paperSystem.SetContent(paper, "абзац про еду\nSCP-131", setup.Body);
            paperSystem.SetContent(paper, "правка\nSCP-131", knowledgeableWriter);
        });

        await pair.Server.WaitPost(() =>
        {
            pair.Server.EntMan.EventBus.RaiseLocalEvent(
                paper,
                new BeforeActivatableUIOpenEvent(setup.Body),
                true);
        });

        await pair.RunTicksSync(2);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp131, out _), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ExamineHidesUnknownScpAndRevealsItAfterUnlock()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var probe = pair.Client.System<ScpKnowledgeExamineProbeSystem>();
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();
        EntityUid speaker = default;

        EntityUid scp173 = default;
        await pair.Server.WaitPost(() =>
        {
            var coordinates = pair.Server.EntMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            scp173 = pair.Server.EntMan.SpawnEntity("Scp173", coordinates);
            speaker = pair.Server.EntMan.SpawnEntity(PlayerPrototype, coordinates);
        });

        await pair.RunTicksSync(5);

        var netEntity = pair.Server.EntMan.GetNetEntity(scp173);

        await pair.Client.WaitPost(() => probe.Reset());
        await pair.Client.WaitPost(() => probe.Request(netEntity, getVerbs: true));
        await pair.RunTicksSync(5);

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.Responses, Has.Count.EqualTo(1));

            var response = probe.Responses.Single();
            Assert.Multiple(() =>
            {
                Assert.That(response.KnowTarget, Is.False);
                Assert.That(response.NameOverride, Is.Null);
                Assert.That(response.Verbs, Is.Null.Or.Empty);
            });
        });

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out var progress), Is.True);
            Assert.That(progress, Is.EqualTo(1));
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.False);
        });

        await pair.Server.WaitPost(() =>
        {
            Assert.That(knowledge.TryGrantKnowledgeProgress(speaker, KnowledgeScp173, 2), Is.True);
            pair.Server.System<ChatSystem>().TrySendInGameICMessage(
                speaker,
                "SCP-173",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp173), Is.True);
        });

        await pair.Client.WaitPost(() => probe.Reset());
        await pair.Client.WaitPost(() => probe.Request(netEntity, getVerbs: true));
        await pair.RunTicksSync(5);

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.Responses, Has.Count.EqualTo(1));

            var response = probe.Responses.Single();
            Assert.Multiple(() =>
            {
                Assert.That(response.KnowTarget, Is.True);
                Assert.That(response.NameOverride, Is.EqualTo(Loc.GetString("scp-knowledge-scp173-name")));
                Assert.That(response.Verbs, Is.Not.Null.And.Not.Empty);
                Assert.That(response.Verbs!.Any(verb => verb.Text == Loc.GetString("scp-knowledge-known-examine-verb")), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OrdinaryEntityAndUnknownTermDoNotAffectKnowledge()
    {
        await using var pair = await GetKnowledgePair();
        var setup = await SpawnPlayerMind(pair);
        var knowledge = pair.Server.System<ScpKnowledgeSystem>();

        EntityUid paper = default;
        await pair.Server.WaitPost(() =>
        {
            var coordinates = pair.Server.EntMan.GetComponent<TransformComponent>(setup.Body).Coordinates;
            paper = pair.Server.EntMan.SpawnEntity("Paper", coordinates);
            pair.Server.System<ChatSystem>().TrySendInGameICMessage(
                setup.Body,
                "абсолютно неизвестный термин",
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                ignoreActionBlocker: true);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(knowledge.TryGetKnowledgeProgress(setup.Body, KnowledgeScp173, out _), Is.False);
                Assert.That(knowledge.TryGetUnknownExamineMessage(setup.Body, paper, out _), Is.False);
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "SCP-173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "scp 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "scp 131"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "сцп 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "объект 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "объекту 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "объектом 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "объекте 173"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "объект 131"), Does.Contain("[color="));
                Assert.That(knowledge.HighlightUnknownKnowledgeText(setup.Body, "абсолютно неизвестный термин"), Is.EqualTo("абсолютно неизвестный термин"));
                Assert.That(knowledge.HasKnowledge(setup.Body, KnowledgeScp131), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    private static Task<TestPair> GetKnowledgePair()
    {
        return PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            Fresh = true,
        });
    }

    private static async Task<PlayerMindSetup> SpawnPlayerMind(TestPair pair, string? jobId = null)
    {
        var map = await pair.CreateTestMap();
        EntityUid mind = default;
        EntityUid body = default;

        await pair.Server.WaitPost(() =>
        {
            var entMan = pair.Server.EntMan;
            var player = pair.Server.ResolveDependency<IPlayerManager>().Sessions.Single();
            var mindSystem = pair.Server.System<MindSystem>();
            var initialCoordinates = map.GridCoords.Offset(new Vector2(0.5f, 0.5f));

            mind = mindSystem.CreateMind(player.UserId);
            body = entMan.SpawnEntity(PlayerPrototype, initialCoordinates);
            mindSystem.TransferTo(mind, body);

            if (jobId != null)
                ApplyJobToBody(pair, mind, body, jobId);
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            var session = pair.Server.ResolveDependency<IPlayerManager>().Sessions.Single();
            Assert.That(session.AttachedEntity, Is.EqualTo(body));
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(pair.Client.EntMan.EntityExists(pair.ToClientUid(body)), Is.True);
        });

        return new PlayerMindSetup(mind, body);
    }

    private static void ApplyJobToBody(TestPair pair, EntityUid mind, EntityUid body, string jobId)
    {
        var roleSystem = pair.Server.System<RoleSystem>();
        var prototypeManager = pair.Server.ResolveDependency<IPrototypeManager>();

        roleSystem.MindAddJobRole(mind, silent: true, jobPrototype: jobId);

        var job = prototypeManager.Index<JobPrototype>(jobId);
        foreach (var special in job.Special)
        {
            special.AfterEquip(body);
        }
    }

    private readonly record struct PlayerMindSetup(EntityUid Mind, EntityUid Body);
}

public sealed class ScpKnowledgeExamineProbeSystem : EntitySystem
{
    public readonly List<ExamineSystemMessages.ExamineInfoResponseMessage> Responses = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ExamineSystemMessages.ExamineInfoResponseMessage>(OnResponse);
    }

    public void Reset()
    {
        Responses.Clear();
    }

    public void Request(NetEntity entity, bool getVerbs)
    {
        RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(entity, 1, getVerbs));
    }

    private void OnResponse(ExamineSystemMessages.ExamineInfoResponseMessage message)
    {
        Responses.Add(message);
    }
}
