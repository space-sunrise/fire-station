namespace Content.Shared._Scp.Trigger.TriggerOnSignalSwitch;

/// <summary>
/// Raised after a signal switch has been successfully activated.
/// Activated contains the switch state after the activation.
/// </summary>
[ByRefEvent]
public record struct SignalSwitchActivatedEvent(bool Activated, EntityUid? User = null, bool Cancelled = false);
