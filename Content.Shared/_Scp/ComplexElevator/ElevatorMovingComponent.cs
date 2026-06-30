using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ElevatorMovingComponent : Component
{
    [AutoNetworkedField]
    public string TargetFloor = string.Empty;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? MovementStartTime;

    [AutoNetworkedField]
    public ElevatorMovementPhase Phase = ElevatorMovementPhase.DoorClosing;
}

[Serializable, NetSerializable]
public enum ElevatorMovementPhase : byte
{
    DoorClosing,
    WaitingForSend,
    Travelling
}
