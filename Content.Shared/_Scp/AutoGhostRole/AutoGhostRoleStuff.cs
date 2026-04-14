namespace Content.Shared._Scp.AutoGhostRole;

/// <summary>
/// Raised on an entity when AutoGhostRoleSystem is about to start a poll for it.
/// Cancel to prevent the poll from starting.
/// </summary>
public sealed class AutoGhostRolePollAttemptEvent : CancellableEntityEventArgs;
