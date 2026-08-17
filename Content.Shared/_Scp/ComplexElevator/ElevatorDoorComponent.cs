using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ElevatorId = string.Empty;

    [DataField, AutoNetworkedField]
    public string Floor = string.Empty;
}
