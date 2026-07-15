using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[Serializable, NetSerializable]
public sealed class ScpOnSoundVisibilityTargetsEvent(NetEntity viewer, NetEntity[] targets) : EntityEventArgs
{
    public readonly NetEntity Viewer = viewer;
    public readonly NetEntity[] Targets = targets;
}
