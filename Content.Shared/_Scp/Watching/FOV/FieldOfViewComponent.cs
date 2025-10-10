using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching.FOV;

/// <summary>
/// Компонент, отвечающий за поле зрения
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class FieldOfViewComponent : Component
{
    public const float MaxOpacity = 0.95f;
    public const float MinOpacity = 0.55f;

    public const float MaxBlurScale = 1f;
    public const float MinBlurScale = 0.25f;

    public const float MaxCooldownCheck = 0.3f;
    public const float MinCooldownCheck = 0.05f;

    /// <summary>
    /// Угол обзора персонажа
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Angle = 180f;

    /// <summary>
    /// "Дополнительный" угол обзора, при котором предметы начинают исчезать из поля зрения, но все еще видны.
    /// Влияет только на скрытие предметов. Визуальный конус не использует это.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AngleTolerance = 14f;

    /// <summary>
    /// Радиус кружочка вокруг персонажа для игнорирования этой области.
    /// Shared-система не будет учитывать это, иначе SCP-173 никогда не сможет подойти к персонажу
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.6f;

    /// <summary>
    /// Пограничные значения кружочка. Аналогично <see cref="AngleTolerance"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.25f;

    /// <summary>
    /// Клиентский параметр, отображающий текущий угол поворота поля зрения.
    /// </summary>
    [ViewVariables]
    public Angle CurrentAngle;

    /// <summary>
    /// Клиентский параметр, отображающий требуемый угол поля зрения, к которому будет стремиться текущий <see cref="CurrentAngle"/> угол при повороте
    /// </summary>
    [ViewVariables]
    public Angle? DesiredViewAngle = null;
}
