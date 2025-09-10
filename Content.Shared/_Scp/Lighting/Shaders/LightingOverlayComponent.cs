using System.Numerics;
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
    public static readonly SpriteSpecifier Mask = new Texture(new ResPath("_Scp/Effects/LightMasks/light_cone.png"));
    public static readonly Vector2 MaskOffset = new (0f, -0.2f);
}
