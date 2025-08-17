using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.AutoOpenCharacterMenu;

public abstract class SharedAutoOpenCharacterMenuSystem : EntitySystem;

[Serializable, NetSerializable]
public sealed class OpenCharacterMenuRequest(NetEntity entity) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
}
