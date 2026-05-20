using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.ComplexElevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorButtonComponent : Component
{
        [DataField, AutoNetworkedField]
    public string ElevatorId = string.Empty;

    [DataField, AutoNetworkedField]
    public ElevatorButtonType ButtonType = ElevatorButtonType.CallButton;

    [DataField, AutoNetworkedField]
    public string Floor = string.Empty;
}
[Serializable, NetSerializable]
public enum ElevatorButtonType
{
    CallButton,
    SendElevatorDown,
    SendElevatorUp,
}