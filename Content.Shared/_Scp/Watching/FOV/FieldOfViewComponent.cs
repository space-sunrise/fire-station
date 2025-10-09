using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching.FOV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class FieldOfViewComponent : Component
{
    public const float MaxOpacity = 0.95f;
    public const float MinOpacity = 0.55f;

    public const float MaxBlurScale = 1f;
    public const float MinBlurScale = 0.25f;

    public const float MaxCooldownCheck = 0.3f;
    public const float MinCooldownCheck = 0.05f;

    [DataField, AutoNetworkedField]
    public float Angle = 180f;

    [DataField, AutoNetworkedField]
    public float AngleTolerance = 14f;

    [DataField, AutoNetworkedField]
    public float ConeOpacity = 0.85f;

    [DataField, AutoNetworkedField]
    public Vector2 Offset = new(0, 0.5f);

    [ViewVariables, AutoNetworkedField]
    public EntityUid? RelayEntity;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.6f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.25f;

    // Clientside, used for lerping view angle
    // and keeping it consistent across all overlays
    [ViewVariables]
    public Angle ViewAngle;

    [ViewVariables]
    public Angle? DesiredViewAngle = null;
}
