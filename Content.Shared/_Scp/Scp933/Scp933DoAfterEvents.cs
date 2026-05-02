using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp933;

[Serializable, NetSerializable]
public sealed partial class Scp933PeelTapeDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class Scp933ApplyTapeDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class Scp933RipTapeDoAfterEvent : DoAfterEvent
{
    public NetEntity ExpectedMask;
    public bool EmergencyMode;

    public override DoAfterEvent Clone()
    {
        return new Scp933RipTapeDoAfterEvent
        {
            ExpectedMask = ExpectedMask,
            EmergencyMode = EmergencyMode,
        };
    }
}
