using Content.Server.Station.Systems;
using Content.Shared.GameTicking;

namespace Content.Server._Sunrise.Roles;

public sealed class RelativeJobsCountSystem : EntitySystem
{
    [Dependency] private readonly StationJobsSystem _jobsSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerJoinedStation);
    }

    private void OnPlayerJoinedStation(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<RelativeJobsCountComponent>(args.Station, out var relativeJobsComponent))
            return;

        var ni = 1;

        foreach (var (targetJob, relativeJobDict) in relativeJobsComponent.Jobs)
        {
            foreach (var (relativeJob, modifier) in relativeJobDict)
            {
                if (_jobsSystem.GetJobs(args.Station).TryGetValue(relativeJob, out var jobCount))
                    continue;

                if (jobCount == null)
                    continue;

                _jobsSystem.TryAdjustJobSlot(args.Station, targetJob, jobCount.Value * modifier, true);
            }
        }
    }
}
