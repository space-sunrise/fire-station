using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Animations.Offset;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OffsetAnimationComponent : Component
{
    [DataField(required: true)]
    public string AnimationKey = "change_this_in_prototype";

    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1f);

    [DataField]
    public Vector2 PeakOffset = new (0f, 1f);

    [DataField]
    public float PeakKeyTime = 0.225f;
}
