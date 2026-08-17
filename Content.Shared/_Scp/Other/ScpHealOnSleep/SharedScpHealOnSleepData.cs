using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.ScpSleep;

[Serializable, NetSerializable]
public enum ScpHealOnSleepLayers : byte
{
    Base = 0
}

[Serializable, NetSerializable]
public enum ScpHealOnSleepVisuals : byte
{
    Sleeping = 0,
}
