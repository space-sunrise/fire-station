using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp106;

public abstract partial class Scp106ValuableActionEvent : InstantActionEvent
{
    [DataField] public FixedPoint2 Cost;
}

public sealed partial class Scp106BackroomsAction : Scp106ValuableActionEvent;

public sealed partial class Scp106RandomTeleportAction : Scp106ValuableActionEvent;
public sealed partial class Scp106BecomePhantomAction : Scp106ValuableActionEvent
{
    [DataField] public EntProtoId PhantomPrototype;
}

public sealed partial class Scp106BecomeTeleportPhantomAction : Scp106ValuableActionEvent
{
    [DataField] public EntProtoId PhantomPrototype;
    [DataField] public TimeSpan Delay;
}
public sealed partial class Scp106ReverseAction : EntityTargetActionEvent
{
    [DataField] public TimeSpan Delay;
}

public sealed partial class Scp106LeavePhantomAction : InstantActionEvent;

public sealed partial class Scp106ShopAction : InstantActionEvent;

public sealed partial class Scp106BoughtPhantomAction : InstantActionEvent
{
    [DataField] public EntProtoId BoughtAction;
}

public sealed partial class Scp106OnUpgradePhantomAction : InstantActionEvent
{
    [DataField] public TimeSpan CooldownReduce;
}

public sealed partial class Scp106PassThroughAction : InstantActionEvent
{
    [DataField] public float Delay;
}

public sealed partial class Scp106BoughtBareBladeAction : InstantActionEvent
{
    [DataField] public EntProtoId BoughtAction;
}

public sealed partial class Scp106BoughtCreatePortal : InstantActionEvent
{
    [DataField] public EntProtoId BoughtAction;
}

public sealed partial class Scp106BareBladeAction : InstantActionEvent
{
    [DataField] public EntProtoId Prototype;
}

#region DoAfters

[Serializable, NetSerializable]
public sealed partial class Scp106BackroomsActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class Scp106RandomTeleportActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class Scp106BecomeTeleportPhantomActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class Scp106ReverseActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class Scp106TeleportationDelayActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class Scp106PassThroughActionEvent : SimpleDoAfterEvent;

#endregion

[Serializable, NetSerializable]
public sealed class WarpingOverlayToggle(bool enable) : EntityEventArgs
{
    public bool Enable = enable;
}
