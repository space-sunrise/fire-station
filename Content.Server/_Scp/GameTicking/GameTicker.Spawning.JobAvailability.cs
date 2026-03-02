using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private bool IsAssignedRoundStartJobAvailable(
        EntityUid station,
        ProtoId<JobPrototype> jobId,
        HashSet<ProtoId<JobPrototype>> restrictedRoles)
    {
        if (restrictedRoles.Contains(jobId))
            return false;

        if (!_stationJobs.TryGetJobSlot(station, jobId.Id, out var slots))
            return false;

        return slots == null || slots > 0;
    }
}
