using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Scp.Lighting.Shaders;

/// <summary>
/// This is used for LightOverlay
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LightingOverlayComponent : Component
{
    [DataField]
    public SpriteSpecifier Sprite = new Texture(new ResPath("_Scp/Effects/LightMasks/light_half_oval.png"));

    [DataField]
    public float Offsetx = 0f;

    [DataField]
    public float Offsety = -0.2f;

    [DataField]
    public Color? Color;
}
