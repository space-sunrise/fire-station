using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.ScpSleep;

[Serializable, NetSerializable]
public enum ScpSleepLayers : byte
{
    Base = 0
}

[Serializable, NetSerializable]
public enum ScpSleepVisuals : byte
{
    Sleeping = 0,
}
