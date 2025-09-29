using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ElevatorPointComponent : Component
{
    [DataField]
    public string FloorId = "";
}