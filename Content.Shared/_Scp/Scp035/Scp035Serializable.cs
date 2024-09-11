using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp035;

[Serializable, NetSerializable]
public enum MaskOrderType : byte
{
    Stay,
    Follow,
    Kill,
    Loose
}

public sealed partial class MaskRaiseArmyActionEvent : InstantActionEvent;

public sealed partial class MaskOrderActionEvent : InstantActionEvent
{
    [DataField("type")]
    public MaskOrderType Type;
}

public sealed partial class MaskStunActionEvent : EntityTargetActionEvent;
