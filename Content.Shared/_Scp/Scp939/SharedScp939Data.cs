using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp939;

public sealed partial class Scp939GasAction : InstantActionEvent;
public sealed partial class Scp939SleepAction : InstantActionEvent;

[Serializable, NetSerializable]
public enum Scp939Layers : byte
{
    Base = 0
}

[Serializable, NetSerializable]
public enum Scp939Visuals : byte
{
    Sleeping = 0,
}
