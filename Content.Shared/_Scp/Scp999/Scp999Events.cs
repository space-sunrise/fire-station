using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp999;

public sealed partial class EntityFedEvent(EntityUid food) : EntityEventArgs
{
    public EntityUid Food = food;
}

#region Abilities

// Rest
public sealed partial class Scp999RestActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class Scp999RestEvent(NetEntity ent, string state) : EntityEventArgs
{
    public NetEntity NetEntity = ent;
    public string TargetState = state;
}

// Wall
public sealed partial class Scp999WallifyActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class Scp999WallifyEvent(NetEntity ent, string state) : EntityEventArgs
{
    public NetEntity NetEntity = ent;
    public readonly string TargetState = state;
}

public sealed class Scp999ChangeStateAttemptEvent(Scp999States state) : CancellableEntityEventArgs
{
    public readonly Scp999States TargetState = state;
}

public sealed class Scp999ChangedStateEvent(Scp999States state) : HandledEntityEventArgs
{
    public readonly Scp999States TargetState = state;
}


#endregion
