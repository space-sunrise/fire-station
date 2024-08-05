using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp999;

[Serializable, NetSerializable]
public sealed class Scp999WallifyEvent(NetEntity ent, string state) : EntityEventArgs
{
    public NetEntity NetEntity = ent;
    public readonly string TargetState = state;
}
