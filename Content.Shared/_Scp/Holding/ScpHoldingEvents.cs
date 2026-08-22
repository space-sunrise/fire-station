using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Holding;

public sealed partial class ScpHoldBreakoutAlertEvent : BaseAlertEvent;

[ByRefEvent]
public record struct ScpHoldAttemptEvent(EntityUid Holder, EntityUid Target)
{
    public bool Cancelled;
}

[ByRefEvent]
public readonly record struct ScpHoldBreakoutEvent(bool ViaMovement, bool WasFullHold, bool AppliedImmunity);

[Serializable, NetSerializable]
public sealed partial class ScpHoldBreakoutDoAfterEvent : SimpleDoAfterEvent
{
    public bool ViaMovement;

    public ScpHoldBreakoutDoAfterEvent(bool viaMovement = false)
    {
        ViaMovement = viaMovement;
    }
}
