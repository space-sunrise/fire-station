using System.Linq;
using Content.Server._Scp.FreeScp;
using Content.Server.EUI;
using Content.Server.Fax;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared._Scp.FreeScp;
using Content.Shared._Scp.GameTicking.Rules;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class FreeScpRuleSystem : GameRuleSystem<FreeScpRuleComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private readonly List<JobPrototype> _cachedScpJobs = new();

    public override void Initialize()
    {
        base.Initialize();
        CacheScpJobs();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(_ => CacheScpJobs());
    }

    protected override void Started(EntityUid uid, FreeScpRuleComponent comp, GameRuleComponent rule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, rule, args);
        comp.Phase = FreeScpRulePhase.WaitingForCheck;
        comp.Deadline = _timing.CurTime + comp.CheckDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FreeScpRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (comp.Phase == FreeScpRulePhase.Finished)
                continue;

            if (comp.Deadline is not { } deadline || _timing.CurTime < deadline)
                continue;

            comp.Deadline = null;

            switch (comp.Phase)
            {
                case FreeScpRulePhase.WaitingForCheck:
                    HandleInitialCheck(uid, comp);
                    break;
                case FreeScpRulePhase.PollOpen:
                    FinishPoll(uid, comp);
                    break;
                case FreeScpRulePhase.WaitingForTransfer:
                    ExecuteTransfer(uid, comp);
                    break;
            }
        }
    }

    private void CacheScpJobs()
    {
        _cachedScpJobs.Clear();
        var scpCompName = _componentFactory.GetComponentName(typeof(ScpComponent));

        foreach (var job in _prototype.EnumeratePrototypes<JobPrototype>())
        {
            if (job.JobEntity == null)
                continue;

            if (!_prototype.TryIndex(job.JobEntity, out var entity))
                continue;

            if (!entity.Components.ContainsKey(scpCompName))
                continue;

            _cachedScpJobs.Add(job);
        }
    }

    private void HandleInitialCheck(EntityUid uid, FreeScpRuleComponent comp)
    {
        if (AnyScpInRound())
        {
            comp.Phase = FreeScpRulePhase.Finished;
            return;
        }

        StartPoll(uid, comp);
    }

    private bool AnyScpInRound()
    {
        var query = EntityQueryEnumerator<ScpComponent>();
        return query.MoveNext(out _, out _);
    }

    private void StartPoll(EntityUid uid, FreeScpRuleComponent comp)
    {
        comp.Phase = FreeScpRulePhase.PollOpen;
        comp.Deadline = _timing.CurTime + comp.PollDuration;
        comp.Acceptors.Clear();

        foreach (var session in _players.Sessions)
        {
            var eui = new FreeScpPollEui(session, (s, accepted) =>
            {
                if (!accepted)
                    return;

                if (comp.Phase == FreeScpRulePhase.PollOpen)
                    comp.Acceptors.Add(s.UserId);
            });
            _eui.OpenEui(eui, session);
        }
    }

    private void FinishPoll(EntityUid uid, FreeScpRuleComponent comp)
    {
        if (comp.Acceptors.Count == 0)
        {
            comp.Phase = FreeScpRulePhase.Finished;
            SendDirectorFax();
            return;
        }

        TryNextCandidate(uid, comp);
    }

    private void TryNextCandidate(EntityUid uid, FreeScpRuleComponent comp)
    {
        var availableJobs = GetAllowedScpJobs();
        if (availableJobs.Count == 0)
        {
            comp.Phase = FreeScpRulePhase.Finished;
            SendDirectorFax();
            return;
        }

        while (comp.Acceptors.Count > 0)
        {
            var candidateId = _random.Pick(comp.Acceptors);
            comp.Acceptors.Remove(candidateId);

            if (!_players.TryGetSessionById(candidateId, out var session))
                continue;

            var job = _random.Pick(availableJobs);
            comp.Winner = candidateId;
            comp.WinnerScpJobId = job.ID;
            comp.Phase = FreeScpRulePhase.WaitingForTransfer;
            comp.Deadline = _timing.CurTime + comp.TransferDelay;

            var transferEui = new FreeScpTransferEui(session, job.LocalizedName,
                (s, choice) => HandleTransferChoice(uid, choice));
            _eui.OpenEui(transferEui, session);
            return;
        }

        comp.Phase = FreeScpRulePhase.Finished;
        SendDirectorFax();
    }

    private void HandleTransferChoice(EntityUid uid, FreeScpTransferMessage.Choice choice)
    {
        if (!TryComp<FreeScpRuleComponent>(uid, out var comp))
            return;

        if (comp.Phase != FreeScpRulePhase.WaitingForTransfer)
            return;

        switch (choice)
        {
            case FreeScpTransferMessage.Choice.Now:
                comp.Deadline = _timing.CurTime;
                break;
            case FreeScpTransferMessage.Choice.Wait:
                break;
            case FreeScpTransferMessage.Choice.Decline:
                ResetWinnerState(comp);
                TryNextCandidate(uid, comp);
                break;
        }
    }

    private void ExecuteTransfer(EntityUid uid, FreeScpRuleComponent comp)
    {
        comp.Phase = FreeScpRulePhase.Finished;

        if (comp.Winner == null || comp.WinnerScpJobId == null
                                || !_prototype.TryIndex<JobPrototype>(comp.WinnerScpJobId, out var job)
                                || job.JobEntity == null)
        {
            ResetWinnerState(comp);
            return;
        }

        if (!_players.TryGetSessionById(comp.Winner.Value, out _))
        {
            ResetWinnerState(comp);
            comp.Phase = FreeScpRulePhase.PollOpen;
            TryNextCandidate(uid, comp);
            return;
        }

        var coords = GameTicker.GetObserverSpawnPoint();
        if (coords == default)
        {
            ResetWinnerState(comp);
            return;
        }
        var scpEntity = Spawn(job.JobEntity, coords);
        var (mindId, mind) = _mind.GetOrCreateMind(comp.Winner.Value);
        _mind.TransferTo(mindId, scpEntity, ghostCheckOverride: true, mind: mind);

        ResetWinnerState(comp);
    }

    private static void ResetWinnerState(FreeScpRuleComponent comp)
    {
        comp.Winner = null;
        comp.WinnerScpJobId = null;
        comp.Deadline = null;
    }

    private void SendDirectorFax()
    {
        var content = Loc.GetString("free-scp-no-volunteers-fax-content");
        var name = Loc.GetString("free-scp-no-volunteers-fax-name");
        var printout = new FaxPrintout(content, name);

        var query = EntityQueryEnumerator<ScpDirectorFaxComponent, FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out _, out var faxComp))
        {
            _fax.Receive(faxUid, printout, component: faxComp);
            break;
        }
    }

    private List<JobPrototype> GetAllowedScpJobs()
    {
        var stations = _station.GetStationsSet();
        var result = new List<JobPrototype>();

        foreach (var job in _cachedScpJobs)
        {
            if (!stations.Any())
            {
                result.Add(job);
                continue;
            }

            foreach (var station in stations)
            {
                if (!_stationJobs.TryGetJobSlot(station, job, out var slots))
                {
                    result.Add(job);
                    break;
                }

                if (slots > 0)
                {
                    result.Add(job);
                    break;
                }
            }
        }

        return result;
    }
}
