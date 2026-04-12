using Robust.Shared.Network;

namespace Content.Shared._Scp.GameTicking.Rules;

public enum FreeScpRulePhase
{
    WaitingForCheck,
    PollOpen,
    WaitingForTransfer,
    Finished
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class FreeScpRuleComponent : Component
{
    /// <summary>
    /// How long after round start before checking for SCPs.
    /// </summary>
    [DataField]
    public TimeSpan CheckDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long the poll stays open.
    /// </summary>
    [DataField]
    public TimeSpan PollDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the winner has before being forcibly transferred.
    /// </summary>
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromMinutes(3);

    [ViewVariables]
    public FreeScpRulePhase Phase = FreeScpRulePhase.WaitingForCheck;

    [ViewVariables, AutoPausedField]
    public TimeSpan? Deadline;

    [ViewVariables]
    public HashSet<NetUserId> Acceptors = new();

    [ViewVariables]
    public NetUserId? Winner;

    [ViewVariables]
    public string? WinnerScpJobId;
}
