using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Server.Station.Components;
using Content.Shared._Scp.Chaos;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class ChaosRaidRuleSystem : GameRuleSystem<ChaosRaidRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
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
        var winText = Loc.GetString($"chaos-raid-{component.WinType.ToString().ToLower()}");
        args.AddLine(winText);

        foreach (var cond in component.WinConditions)
        {
            var text = Loc.GetString($"chaos-raid-cond-{cond.ToString().ToLower()}");
            args.AddLine(text);
        }

        args.AddLine(Loc.GetString("chaos-raid-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("chaos-raid-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
        args.AddLine("");
    }

    private void OnMapInit(Entity<MobChaosRaiderComponent> ent, ref MapInitEvent args)
    {

        RemCompDeferred<FearComponent>(ent); // ПОВСТАНЦЫ БЕЗ СТРАХА!
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

    private void CheckRoundShouldEnd()
    {

    }
}
