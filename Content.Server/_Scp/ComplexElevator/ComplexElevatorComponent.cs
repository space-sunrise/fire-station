using Robust.Shared.Audio;

namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ComplexElevatorComponent : Component
{
    [DataField]
    public string ElevatorId = string.Empty;

    [DataField]
    public string CurrentFloor = "IntermediateFloor";

    [DataField]
    public List<string> Floors = new();

    [DataField]
    public string IntermediateFloorId = "IntermediateFloor";

    [DataField]
    public bool UseIntermediateFloor = true;

    [DataField]
    public TimeSpan SendDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan IntermediateDelay = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan DoorCloseDelay = TimeSpan.FromSeconds(0.3);

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_Scp/Machines/Elevator/ElevatorClose.ogg");

    [DataField]
    public SoundSpecifier TravelSound = new SoundPathSpecifier("/Audio/_Scp/Machines/Elevator/Moving.ogg");

    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/_Scp/Machines/Elevator/Beep-elevator.ogg");

    [DataField]
    public float DoorBlockCheckRange = 0.6f;

    [DataField]
    public bool TeleportBuckled = true;

    [DataField]
    public bool TeleportPulled = true;

    [DataField]
    public bool CrushEntitiesOnArrival = true;

    [DataField]
    public float CrushDamage = 2000f;
}
