using Content.Shared._Scp.Helpers;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Shaders.Grain;

/// <summary>
/// Компонент, отвечающий за параметры шейдера зернистости.
/// Наличие компонента необходимо для работы шейдера.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class GrainOverlayComponent : Component
{
    /// <summary>
    /// Максимальные и минимальные значения базовой силы шейдера зернистости.
    /// Эти пороги используются для настроек клиента и позволяет выбрать доступный диапазон
    /// </summary>
    public static readonly MinMaxExtended BaseStrengthLimit = new (70, 140);

    /// <summary>
    /// Базовая сила зернистости.
    /// Она определяет то, какой силы будет шейдер при стандартных условиях.
    /// Может быть настроена клиентом на свое усмотрение.
    /// </summary>
    /// <remarks>
    /// КЛИЕНТСКИЙ ПАРАМЕТР -> НЕ МЕНЯТЬ С СЕРВЕРА!!
    /// </remarks>
    [ViewVariables]
    public int BaseStrength
    {
        get => _baseStrength;
        set => _baseStrength = Math.Clamp(value, BaseStrengthLimit.Min, BaseStrengthLimit.Max);
    }

    /// <summary>
    /// Дополнительная сила шейдера зернистости.
    /// Складывается с <see cref="BaseStrength"/> в <see cref="CurrentStrength"/>.
    /// Не может быть настроена клиентом, настраивается различными системами извне в качестве негативного эффекта.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public int AdditionalStrength;

    /// <summary>
    /// Текущая сила шейдера зернистости.
    /// Складывается из <see cref="BaseStrength"/> и <see cref="AdditionalStrength"/>.
    /// Определяет реальную силу шейдера, учитывая все параметры.
    /// </summary>
    [ViewVariables]
    public int CurrentStrength => BaseStrength + AdditionalStrength;

    private int _baseStrength;
}
