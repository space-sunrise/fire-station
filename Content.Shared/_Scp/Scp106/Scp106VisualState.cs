using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp106;

[Serializable, NetSerializable]
public enum Scp106Visuals : byte
{
    Visuals = 0
}

[Serializable, NetSerializable]
public enum Scp106VisualsState : byte
{
    Default = 0,

    Entering = 1,

    Exiting = 2
}
