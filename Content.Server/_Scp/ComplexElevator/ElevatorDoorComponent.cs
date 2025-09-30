namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ElevatorDoorComponent : Component
{
    [DataField]
    public string ElevatorId = "";

    [DataField]
    public string Floor = "";
}