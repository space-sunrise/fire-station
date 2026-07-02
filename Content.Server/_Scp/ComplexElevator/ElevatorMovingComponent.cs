namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ElevatorMovingComponent : Component
{
    public string TargetFloor = string.Empty;
    public TimeSpan? MovementStartTime;
    public ElevatorMovementPhase Phase = ElevatorMovementPhase.DoorClosing;
}

public enum ElevatorMovementPhase : byte
{
    DoorClosing,
    WaitingForSend,
    Travelling
}
