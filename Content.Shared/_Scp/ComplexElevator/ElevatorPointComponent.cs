using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FloorId = string.Empty;
}
