using Content.Server.Players.PlayTimeTracking;
using Content.Shared._Scp.Other.AutoOpenCharacterMenu;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.AutoOpenCharacterMenu;

public sealed class AutoOpenCharacterMenuSystem : SharedAutoOpenCharacterMenuSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playtime = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null || !_prototype.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        var playtime = _playtime.GetPlayTimeForTracker(ev.Player, job.PlayTimeTracker);

        if (playtime != TimeSpan.Zero)
            return;

        var playerEntity = GetNetEntity(ev.Mob);
        RaiseNetworkEvent(new OpenCharacterMenuRequest(playerEntity));
    }
}
