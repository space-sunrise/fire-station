using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching.FOV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FieldOfViewComponent : Component
{
    public const float MaxOpacity = 0.95f;
    public const float MinOpacity = 0.55f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Angle = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float AngleTolerance = 18f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeOpacity = 0.85f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 Offset = new(0, 0.5f);
}
