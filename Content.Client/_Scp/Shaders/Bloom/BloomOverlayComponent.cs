﻿using System.Numerics;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._Scp.Shaders.Bloom;

/// <summary>
/// Компонент, отвечающий за отрисовку эффект свечения у лампочек.
/// </summary>
/// TODO: Фонарики с шейдерами.
/// Нужно сделать:
/// 1. Offset как datafield
/// 2. Настраиваемую для предмета яркость
/// 3. Логику "игрок взял в руки -> яркость уменьшилась или эффект пропал"
[RegisterComponent]
public sealed partial class BloomOverlayVisualsComponent : Component
{
    [ViewVariables]
    public static readonly SpriteSpecifier Cone = new Texture(new ResPath("_Scp/Effects/LightMasks/light_cone.png"));
    [ViewVariables]
    public static readonly Vector2 ConeOffset = new (0f, -0.2f);

    [ViewVariables]
    public const float DefaultConeBaseHaze = 0.4f;
    [ViewVariables]
    public const float DefaultConeHuetaDivisor = 0.225f;

    [ViewVariables]
    public static readonly SpriteSpecifier Point = new Texture(new ResPath("_Scp/Effects/LightMasks/light_point.png"));
    [ViewVariables]
    public static readonly Vector2 PointOffset = new (0f, 0.45f);

    [ViewVariables]
    public const float DefaultPointBaseHaze = 0.8f;
    [ViewVariables]
    public const float DefaultPointHuetaDivisor = 0.05f;
}
