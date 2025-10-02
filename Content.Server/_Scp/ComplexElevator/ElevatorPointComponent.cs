using Robust.Shared.GameStates;

namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ElevatorPointComponent : Component
{
    [DataField]
    public string FloorId = "";
}