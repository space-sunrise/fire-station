using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp096;

[Serializable, NetSerializable]
public enum Scp096Visuals : byte
{
    Visuals = 0,
}

[Serializable, NetSerializable]
public enum Scp096VisualsState : byte
{
    Agro = 0,
    Idle = 1,
    IdleAgro = 2,
    Walking = 4,
    Dead = 8,
    Running = 16,
}
