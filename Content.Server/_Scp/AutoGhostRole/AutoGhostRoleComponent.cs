namespace Content.Server._Scp.AutoGhostRole;

/// <summary>
/// When added to an entity, automatically starts a body takeover poll
/// after the player has been disconnected for <see cref="DisconnectDelay"/>.
/// Generic — not SCP-specific. Can be placed on any entity that should have automatic ghost role replacement.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AutoGhostRoleComponent : Component
{
    /// <summary>
    /// How long after player disconnect before the poll starts.
    /// </summary>
    [DataField]
    public TimeSpan DisconnectDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long each poll phase stays open.
    /// </summary>
    [DataField]
    public TimeSpan PollDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long a living player winner has before being forcibly transferred.
    /// </summary>
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Whether this entity has ever had a player controlling it.
    /// Prevents admin-spawned entities from being given to players automatically.
    /// </summary>
    [ViewVariables]
    public bool EverHadPlayer = false;

    /// <summary>
    /// Timestamp when the player disconnected. Null if a player is currently attached or no player has left yet.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? DisconnectedAt = null;
}
