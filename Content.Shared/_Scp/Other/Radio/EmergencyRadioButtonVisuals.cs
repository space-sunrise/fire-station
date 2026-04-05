using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.Radio;

[Serializable, NetSerializable]
public enum EmergencyRadioButtonVisuals : byte
{
    Pressed,
}

[Serializable, NetSerializable]
public enum EmergencyRadioButtonVisualLayers : byte
{
    Base,
    Cover,
}
