using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComplexElevatorComponent : Component
{
    [DataField]
    public string ElevatorId = "";

    [DataField]
    public string CurrentFloor = "SurfaceFloor";

    [DataField]
    public string FirstPointId = "ComplexFloor";

    [DataField]
    public string IntermediateFloorId = "IntermediateFloor";

    [DataField]
    public string SecondPointId = "SurfaceFloor";

    [DataField]
    public bool IsMoving = false;

    [DataField]
    public TimeSpan SendDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan IntermediateDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public HashSet<EntityUid> EntitiesOnElevator = new();
}