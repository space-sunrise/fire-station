using Content.Shared.Actions;

namespace Content.Shared._Scp.Other.Events;

[ByRefEvent]
public sealed class ScpReleaseGasActionAttemptEvent : CancellableEntityEventArgs;

public sealed partial class ScpReleaseGasActionEvent : InstantActionEvent;

public sealed partial class ScpRememberPhraseActionEvent : InstantActionEvent;

public sealed partial class ScpSleepActionEvent : InstantActionEvent;
