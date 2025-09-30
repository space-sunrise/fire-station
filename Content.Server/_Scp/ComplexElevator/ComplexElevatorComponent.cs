namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ComplexElevatorComponent : Component
{
    [DataField]
    public string ElevatorId = "";

    [DataField]
    public string CurrentFloor = "IntermediateFloor";

    [DataField]
    public string FirstPointId = "ComplexFloor";

    [DataField]
    public string IntermediateFloorId = "IntermediateFloor";

    [DataField]
    public string SecondPointId = "SurfaceFloor";

    [DataField]
    public TimeSpan SendDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan IntermediateDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public bool IsMoving = false;

    public HashSet<EntityUid> EntitiesOnElevator = new();
}