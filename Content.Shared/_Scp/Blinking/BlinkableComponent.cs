using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BlinkableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextBlink;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan BlinkEndTime;

    [ViewVariables, AutoNetworkedField]
    public float AdditionalBlinkingTime;

    [DataField]
    public ProtoId<AlertPrototype> BlinkingAlert = "Blinking";

    #region Eye closing

    /// <summary>
    /// Айди прототипа способности закрыть глаза
    /// </summary>
    [DataField]
    public EntProtoId EyeToggleAction = "ActionToggleEyes";

    /// <summary>
    /// Сущность способности закрыть глаза вручную
    /// </summary>
    public EntityUid? EyeToggleActionEntity;

    /// <summary>
    /// Закрыты ли глаза
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EyesState State = EyesState.Opened;

    [ViewVariables, AutoNetworkedField]
    public bool ManuallyClosed;

    /// <summary>
    /// Сохраненный цвет глаз персонажа.
    /// Используется, чтобы вернуть изначальный цвет глаз после открытия глаз.
    /// Так как во время закрытия цвет глаз меняется на цвет кожи.
    /// </summary>
    public Color? CachedEyesColor;

    [ViewVariables]
    public GameTick? LastClientSideVisualsAttemptTick;

    #endregion

}

/// <summary>
/// Состояние глаз - закрыты/открыты
/// </summary>
/// <remarks>
/// Я не хотел использовать true/false, потому что в коде образуются непонятки
/// И приходится тратить слишком много времени на понимание, как true/false соотносится с закрытыми/открытыми
/// В разных методах можно случайно использовать разные варианты комбинаций true/false к закрытым/открытым что приводит к непонятностям
/// Enum намного более прост в понимании и такой код будет легко читаем
/// </remarks>
public enum EyesState : byte
{
    Closed,
    Opened,
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ShowBlinkableComponent : Component;
