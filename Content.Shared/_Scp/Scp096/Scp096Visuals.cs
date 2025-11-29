using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp096;

[Serializable, NetSerializable]
public enum Scp096VisualsState : byte
{
    Agro = 0,
    Idle = 1,
    Dead = 2,
    Heating = 3,
    DeadToIdle = 4,
    AgroToDead = 5,
}
