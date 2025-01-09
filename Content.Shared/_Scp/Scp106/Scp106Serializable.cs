using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp106;

public sealed partial class Scp106BackroomsAction : InstantActionEvent {}

public sealed partial class Scp106RandomTeleportAction : InstantActionEvent {}

public sealed partial class Scp106BecomePhantomAction : InstantActionEvent {}

public sealed partial class Scp106ReverseAction : EntityTargetActionEvent {}

public sealed partial class Scp106LeavePhantomAction : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed partial class Scp106BackroomsActionEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class Scp106RandomTeleportActionEvent : SimpleDoAfterEvent { }
