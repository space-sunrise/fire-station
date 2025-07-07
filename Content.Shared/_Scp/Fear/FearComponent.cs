using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear;

/// <summary>
/// Компонент, отвечающий за возможность пугаться.
/// Обрабатывает уровни страха и хранит текущий.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FearComponent : Component
{
    /// <inheritdoc cref="FearState"/>
    [AutoNetworkedField, ViewVariables]
    public FearState State = FearState.None;

    /// <summary>
    /// Время, через которое уровень страха понизится.
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan TimeToDecreaseFearLevel = TimeSpan.FromSeconds(15f); // TODO: 3 минуты

    /// <summary>
    /// Следующее время, когда будут понижен уровень страха со временем.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextTimeDecreaseFearLevel = TimeSpan.Zero;

    #region Shader strength

    /// <summary>
    /// Этот словарь описывает зависимость силы шейдера зернистости от уровня страха у владельца компонента.
    /// Эти значения будут суммироваться с другими при добавлении.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<FearState, float> FearBasedGrainStrength = new ()
    {
        { FearState.None , 0f },
        { FearState.Anxiety, 50f },
        { FearState.Fear , 100f },
        { FearState.Terror, 700f },
    };

    /// <summary>
    /// Этот словарь описывает зависимость силы шейдера виньетки от уровня страха у владельца компонента.
    /// Эти значения будут суммироваться с другими при добавлении.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<FearState, float> FearBasedVignetteStrength = new ()
    {
        { FearState.None , 0f },
        { FearState.Anxiety, 30f },
        { FearState.Fear , 70f },
        { FearState.Terror, 400f },
    };

    /// <summary>
    /// Хранит текущие параметры силы шейдера, зависимой от уровня страха.
    /// Каждый шейдер хранится отдельной парой, где ключ компонент шейдера, а значение его fear-based сила.
    /// </summary>
    /// <remarks>
    /// Ключ в виде строки означает имя компонента с силой шейдера.
    /// </remarks>
    [ViewVariables]
    public Dictionary<string, float> CurrentFearBasedShaderStrength = new();

    #endregion

    [DataField, ViewVariables]
    public TimeSpan TimeToGetScaredAgainOnLookAt = TimeSpan.FromSeconds(10f); // TODO: 5 минут
}

/// <summary>
/// Уровни страха. Чем больше значение, тем сильнее страх
/// </summary>
public enum FearState
{
    /// <summary>
    /// Отсутствие страха. Сущность в спокойном состоянии
    /// </summary>
    None,

    /// <summary>
    /// Тревожность. Сущность немного напугана
    /// </summary>
    Anxiety,

    /// <summary>
    /// Страх. Сущность испытает прямой страх от чего-либо
    /// </summary>
    Fear,

    /// <summary>
    /// Неконтролируемый ужас. Сущность невероятно напугана, что-то СЛИШКОМ ужасно, чтобы знать об этом.
    /// </summary>
    Terror,
}
