using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp106;

public sealed partial class Scp106BackroomsAction : InstantActionEvent {}

public sealed partial class Scp106RandomTeleportAction : InstantActionEvent {}

public sealed partial class Scp106BecomePhantomAction : InstantActionEvent {}

public sealed partial class Scp106BecomeTeleportPhantomAction : InstantActionEvent {}

public sealed partial class Scp106ReverseAction : EntityTargetActionEvent {}

public sealed partial class Scp106LeavePhantomAction : InstantActionEvent {}

public sealed partial class Scp106ShopAction : InstantActionEvent {}

public sealed partial class Scp106BoughtPhantomAction : InstantActionEvent {}

public sealed partial class Scp106OnUpgradePhantomAction : InstantActionEvent {}

public sealed partial class Scp106PassThroughAction : InstantActionEvent {}

public sealed partial class Scp106CreatePortalAction : InstantActionEvent {}

public sealed partial class Scp106BoughtBareBladeAction : InstantActionEvent {}

public sealed partial class Scp106BoughtCreatePortal : InstantActionEvent {}

public sealed partial class Scp106BareBladeAction : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed partial class Scp106BackroomsActionEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class Scp106RandomTeleportActionEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class Scp106BecomeTeleportPhantomActionEvent : SimpleDoAfterEvent {}

[Serializable, NetSerializable]
public sealed partial class Scp106ReverseActionEvent : SimpleDoAfterEvent {}

[Serializable, NetSerializable]
public sealed partial class Scp106TeleporationDelayActionEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class Scp106PassThroughActionEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class Scp106CreatePortalEvent : SimpleDoAfterEvent {}
