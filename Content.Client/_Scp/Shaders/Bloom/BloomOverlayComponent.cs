using System.Numerics;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._Scp.Shaders.Bloom;

/// <summary>
/// Компонент, отвечающий за отрисовку эффект свечения у лампочек.
/// </summary>
[RegisterComponent]
public sealed partial class BloomOverlayVisualsComponent : Component
{
    public static readonly SpriteSpecifier Mask = new Texture(new ResPath("_Scp/Effects/LightMasks/light_cone.png"));
    public static readonly Vector2 MaskOffset = new (0f, -0.2f);

    public static readonly SpriteSpecifier Point = new Texture(new ResPath("_Scp/Effects/LightMasks/light_point.png"));
    public static readonly Vector2 PointOffset = new (0f, 0.5f);
}
