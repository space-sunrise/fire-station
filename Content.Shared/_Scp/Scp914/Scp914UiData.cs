using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp914;


[Serializable, NetSerializable]
public sealed partial class Scp914BuiState : BoundUserInterfaceState
{
    public Scp914Mode NewMode { get; set; }
    public bool Active { get; set; }

    public Scp914BuiState(Scp914Mode newMode, bool active)
    {
        NewMode = newMode;
        Active = active;
    }
}

[Serializable, NetSerializable]
public sealed partial class Scp914ChangeModeRequestMessage : BoundUserInterfaceMessage
{
    public Scp914CycleDirection Direction { get; set; }

    public Scp914ChangeModeRequestMessage(Scp914CycleDirection direction)
    {
        Direction = direction;
    }
}

[Serializable, NetSerializable]
public sealed partial class Scp914StartCycleMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public enum Scp914CycleDirection : byte
{
    Left = 0,
    Right = 1,
}

[Serializable, NetSerializable]
public enum Scp914UiKey
{
    Key,
}

