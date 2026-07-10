using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.ShowMessageOnSpawn;

[Serializable, NetSerializable]
public sealed class ShowMessageOnSpawnWindowOpenedEvent : EntityEventArgs
{
    public NetEntity TargetEntity { get; set; }
}
