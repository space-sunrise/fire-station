using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Scp.BodyTakeover;

public enum BodyTakeoverPollPhase
{
    GhostPoll,
    LivingPoll,
    TransferPending,
    Done
}

/// <summary>
/// Added dynamically to an entity when a body takeover poll is active for it.
/// Tracks the current poll state and callbacks.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BodyTakeoverPollStateComponent : Component
{
    [ViewVariables]
    public BodyTakeoverPollPhase Phase = BodyTakeoverPollPhase.GhostPoll;

    [ViewVariables, AutoPausedField]
    public TimeSpan? Deadline;

    [ViewVariables]
    public TimeSpan TransferDelay;

    [ViewVariables]
    public HashSet<NetUserId> Acceptors = [];

    [ViewVariables]
    public NetUserId? Winner;

    [ViewVariables]
    public TimeSpan PollDuration;

    public Action<ICommonSession>? OnSuccess;
    public Action? OnFailed;

    /// <summary>
    /// The phase active when a winner was selected. Used to restore correctly if the winner disconnects.
    /// </summary>
    [ViewVariables]
    public BodyTakeoverPollPhase PhaseBeforeTransfer = BodyTakeoverPollPhase.GhostPoll;

    /// <summary>
    /// How long to wait after the first acceptance before picking a winner from all acceptors.
    /// Prevents the first player to click from always winning.
    /// </summary>
    [DataField]
    public TimeSpan RaffleWindow = TimeSpan.FromSeconds(10);
}
