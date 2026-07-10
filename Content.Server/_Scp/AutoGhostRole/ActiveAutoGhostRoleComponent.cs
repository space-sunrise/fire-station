namespace Content.Server._Scp.AutoGhostRole;

/// <summary>
/// Marker added to an entity when its player has disconnected
/// and the auto ghost role system is waiting to start a poll.
/// Removed when the player reconnects or the poll starts.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveAutoGhostRoleComponent : Component
{
    [ViewVariables, AutoPausedField]
    public TimeSpan DisconnectedAt;
}
