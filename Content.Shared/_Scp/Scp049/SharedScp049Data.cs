using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp049;

public sealed partial class Scp049ResurrectAction : EntityTargetActionEvent;

public sealed partial class Scp049KillResurrectedAction : EntityTargetActionEvent;

public sealed partial class Scp049KillLivingBeingAction : EntityTargetActionEvent;

public sealed partial class Scp049SelfHealAction : InstantActionEvent;

public sealed partial class Scp049HealMinionAction : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class ScpResurrectionDoAfterEvent : SimpleDoAfterEvent;


