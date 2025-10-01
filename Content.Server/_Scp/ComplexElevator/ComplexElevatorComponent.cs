using System.Collections.Generic;

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
    public TimeSpan SendDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan IntermediateDelay = TimeSpan.FromSeconds(5);

    public bool IsMoving = false;
}