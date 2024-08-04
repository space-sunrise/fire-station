using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.PowerCell;
using Content.Shared.Throwing;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Abilities;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedBorgDashSystem : EntitySystem
{

}

// public sealed class BorgLandedEvent(EntityUid uid) : EntityEventArgs
// {
//     public EntityUid Uid { get; set; } = uid;
// }
//
// public sealed class BorgThrownEvent(EntityUid uid) : EntityEventArgs
// {
//     public EntityUid Uid { get; set; } = uid;
// }

[Serializable, NetSerializable]
public sealed class BorgLandedEvent(NetEntity borg) : EntityEventArgs
{
    public readonly NetEntity Borg = borg;
}

[Serializable, NetSerializable]
public sealed class BorgThrownEvent(NetEntity borg) : EntityEventArgs
{
    public readonly NetEntity Borg = borg;
}
