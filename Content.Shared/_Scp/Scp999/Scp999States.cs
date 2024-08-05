using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp999;

[Serializable, NetSerializable]
public enum Scp999States : byte
{
    Default,
    Wall,
    Rest,
}
