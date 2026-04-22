using Robust.Shared.Network;

namespace Content.Shared._Scp.GameTicking.Rules;

public enum FreeScpRulePhase
{
    WaitingForCheck,
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
    /// How long each poll phase stays open.
    /// </summary>
    [DataField]
    public TimeSpan PollDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a living player winner has before being forcibly transferred.
    /// </summary>
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromMinutes(3);

    [ViewVariables]
    public FreeScpRulePhase Phase = FreeScpRulePhase.WaitingForCheck;

    [ViewVariables, AutoPausedField]
    public TimeSpan? Deadline;
}
