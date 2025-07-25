using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Watching.FOV;

[Serializable, NetSerializable]
public sealed class CameraSwitchedEvent(NetEntity actor) : EntityEventArgs
{
    public readonly NetEntity Actor = actor;
}
