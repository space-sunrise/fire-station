using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching.FOV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FieldOfViewComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Angle = 240f;
}
