using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FloorId = string.Empty;
}
