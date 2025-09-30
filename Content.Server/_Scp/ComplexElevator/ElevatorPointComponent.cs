namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ElevatorPointComponent : Component
{
    [DataField(serverOnly: true)]
    public string FloorId = "";
}