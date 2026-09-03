using System.Numerics;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._Scp.Shaders.ConeLighting;

/// <summary>
/// Компонент, отвечающий за отрисовку эффекта свечения в виде конусов у лампочек.
/// </summary>
/// TODO: Фонарики с шейдерами.
/// Нужно сделать:
/// 1. Offset как datafield
/// 2. Настраиваемую для предмета яркость
/// 3. Логику "игрок взял в руки -> яркость уменьшилась или эффект пропал"
[RegisterComponent]
public sealed partial class ConeLightingOverlayVisualsComponent : Component
{
    [ViewVariables]
    public static readonly SpriteSpecifier Cone = new Rsi(new ResPath("_Scp/Effects/LightMasks/128.rsi"), "light_cone");
    [ViewVariables]
    public static readonly Vector2 ConeOffset = new (0f, -0.2f);

    [ViewVariables]
    public const float DefaultConeBaseHaze = 0.4f;
    [ViewVariables]
    public const float DefaultConeHuetaDivisor = 0.225f;
}
