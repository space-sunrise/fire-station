using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp173;

public sealed partial class Scp173BlindAction : InstantActionEvent;

public sealed partial class Scp173ClogAction : InstantActionEvent;

public sealed partial class Scp173DamageStructureAction : InstantActionEvent;

public sealed partial class Scp173FastMovementAction : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class Scp173StartBlind : SimpleDoAfterEvent;
