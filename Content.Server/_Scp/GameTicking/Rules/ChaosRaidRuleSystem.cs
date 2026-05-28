using System.Linq;
using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Shared._Scp.Chaos;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class ChaosRaidRuleSystem : GameRuleSystem<ChaosRaidRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobChaosRaiderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MobChaosRaiderComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<MobChaosRaiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobChaosRaiderComponent, EntityZombifiedEvent>(OnRaiderZombified);

        SubscribeLocalEvent<ChaosRaidRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntSelected);
        SubscribeLocalEvent<ChaosRaiderRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    protected override void Started(EntityUid uid,
        ChaosRaidRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(component.Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        component.TargetComplex = eligible[0];
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ChaosRaidRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        CheckRoundShouldEnd((uid, component));
        var winText = Loc.GetString($"chaos-raid-{component.WinType.ToString().ToLower()}");
        args.AddLine(winText);

        foreach (var cond in component.WinConditions)
        {
            var text = Loc.GetString($"chaos-raid-cond-{cond.ToString().ToLower()}");
            args.AddLine(text);
        }

        args.AddLine(Loc.GetString("chaos-raid-list-completed-objectives-count",
            ("objectivesCount", component.ObjectivesCount), ("completedCount", component.CompletedObjectivesCount)
        ));
        args.AddLine(Loc.GetString("chaos-raid-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("chaos-raid-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
        args.AddLine("");
    }

    // TODO: Переделать эту систему, что бы она работала иначе, брав точки отдельно на шаттле, а так же базе повстанцев хаоса, а не на всей карте.
    private void OnMapInit(Entity<MobChaosRaiderComponent> ent, ref MapInitEvent args)
    {
        RemCompDeferred<FearComponent>(ent); // ПОВСТАНЦЫ БЕЗ СТРАХА!

        var query = EntityQuery<StealAreaComponent, TransformComponent>();
        if (!query.Any())
            return;

        if (!_mind.TryGetMind(ent, out var mindId, out _))
            return;

        // Все StealArea-точки на этой карте (база повстанец хаоса) пренадлежат Повстанцам Хаоса
        foreach (var (stealComp, xform) in query)
        {
            if (Transform(ent).MapID == xform.MapID)
                stealComp.Owners.Add(mindId);
        }
    }

    private void OnAfterAntagEntSelected(Entity<ChaosRaidRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var target = (ent.Comp.TargetComplex is not null) ? Name(ent.Comp.TargetComplex.Value) : "the target";

        _antag.SendBriefing(args.Session,
            Loc.GetString("chaos-raider-welcome",
                ("station", target),
                ("name", Name(ent))),
            Color.Red,
            ent.Comp.GreetSoundNotification);

        ent.Comp.RoundstartRaidersCount += 1;
    }

    private void OnGetBriefing(Entity<ChaosRaiderRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("chaos-raider-briefing"));
    }

    private void OnComponentRemove(Entity<MobChaosRaiderComponent> ent, ref ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnMobStateChanged(Entity<MobChaosRaiderComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnRaiderZombified(Entity<MobChaosRaiderComponent> ent, ref EntityZombifiedEvent args)
    {
        RemCompDeferred<MobChaosRaiderComponent>(ent);
    }

    private void SetWinType(Entity<ChaosRaidRuleComponent> ent, ChaosWinType type, bool endRound = true)
    {
        ent.Comp.WinType = type;

        if (endRound && (type == ChaosWinType.CrewMajor || type == ChaosWinType.ChaosMajor))
            _roundEnd.EndRound();
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var chaos, out _))
        {
            CheckRoundShouldEnd((uid, chaos));
        }
    }

    private void CheckRoundShouldEnd(Entity<ChaosRaidRuleComponent> ent)
    {
        if (ent.Comp.WinType == ChaosWinType.CrewMajor || ent.Comp.WinType == ChaosWinType.ChaosMajor)
            return;

        CalculateProgress(ent.Comp, out var operativesAlives);
        var halfRaiders = Math.Max(1, (ent.Comp.RoundstartRaidersCount + 1) / 2);
        var halfObjectives = Math.Max(1, (ent.Comp.ObjectivesCount + 1) / 2);

        if (ent.Comp.CompletedObjectivesCount >= ent.Comp.ObjectivesCount &&
            !ent.Comp.WinConditions.Contains(ChaosWinCondition.ChaosRaidersCompleteAllObjectives))
            ent.Comp.WinConditions.Add(ChaosWinCondition.ChaosRaidersCompleteAllObjectives);

        if (ent.Comp.WinConditions.Contains(ChaosWinCondition.ChaosRaidersCompleteAllObjectives) &&
            ent.Comp.RoundstartRaidersCount > 1 &&
            operativesAlives >= halfRaiders)
            SetWinType(ent, ChaosWinType.ChaosMajor, false);

        if (ent.Comp.WinType != ChaosWinType.ChaosMajor &&
            ent.Comp.CompletedObjectivesCount >= halfObjectives)
            SetWinType(ent, ChaosWinType.ChaosMinor, false);

        if (ent.Comp.WinType != ChaosWinType.ChaosMajor &&
            operativesAlives < halfRaiders)
            SetWinType(ent, ChaosWinType.CrewMinor, false);

        if (operativesAlives > 0)
            return;

        if (!ent.Comp.WinConditions.Contains(ChaosWinCondition.CrewKillAllChaosRaiders))
            ent.Comp.WinConditions.Add(ChaosWinCondition.CrewKillAllChaosRaiders);

        if (ent.Comp.WinType != ChaosWinType.ChaosMajor)
            SetWinType(ent, ChaosWinType.CrewMajor, false);

        if (ent.Comp.RoundEndBehavior == RoundEndBehavior.Nothing)
            return;

        _roundEnd.DoRoundEndBehavior(ent.Comp.RoundEndBehavior,
        ent.Comp.EvacShuttleTime,
        ent.Comp.RoundEndTextSender,
        ent.Comp.RoundEndTextShuttleCall,
        ent.Comp.RoundEndTextAnnouncement);

        ent.Comp.RoundEndBehavior = RoundEndBehavior.Nothing;
    }

    private void CalculateProgress(ChaosRaidRuleComponent ruleComp, out int operativesAlives)
    {
        operativesAlives = 0;
        var objectives = new Dictionary<string, float>();
        var completedObjectivesCount = 0;
        var query = EntityQueryEnumerator<MobChaosRaiderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TryComp<MobStateComponent>(uid, out var stateComp) &&
                stateComp.CurrentState == MobState.Alive)
                operativesAlives++;

            if (!_mind.TryGetMind(uid, out _, out var mind))
                continue;

            foreach (var objective in mind.Objectives)
            {
                var objectiveKey = GetObjectiveKey(objective);
                var progress = _objectives.GetProgress(objective, (uid, mind)) ?? 0f;

                objectives.TryAdd(objectiveKey, 0f);
                objectives[objectiveKey] += progress;
            }
        }

        foreach (var (key, progress) in objectives)
        {
            if (progress >= 0.999f)
                completedObjectivesCount++;
        }

        ruleComp.ObjectivesCount = objectives.Count;
        ruleComp.CompletedObjectivesCount = completedObjectivesCount;
    }

    private string GetObjectiveKey(EntityUid objective)
    {
        var protoId = Prototype(objective)?.ID ?? "Unknown";
        if (TryComp<TargetObjectiveComponent>(objective, out var targetComp))
            return targetComp.Target != null
                ? $"{protoId}_kill_{targetComp.Target}"
                : protoId;

        if (TryComp<StealConditionComponent>(objective, out var stealComp))
            return $"{protoId}_steal_{stealComp.StealGroup}";

        return protoId;
    }
}
