using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

/// <summary>
/// Компонент, отвечающий за возможность моргать, закрыть и открывать глаза.
/// Позволяет систем некоторых SCP объектов взаимодействовать с ними.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BlinkableComponent : Component
{
    /// <summary>
    /// Время, между морганием. Сколько потребуется времени, чтобы глаза снова закрылись.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkingInterval = TimeSpan.FromSeconds(8f);

    /// <summary>
    /// Длительность моргания. Сколько игрок проведет с закрытыми глазами во время моргания
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkingDuration = TimeSpan.FromSeconds(2.4f);

    /// <summary>
    /// Вариативность интервала между морганием.
    /// Добавляется к интервалу моргания как случайное число от 0 до этого.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlinkingIntervalVariance = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Время следующего моргания.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextBlink;

    /// <summary>
    /// Время окончания моргания.
    /// То есть момент открытия глаз после их закрытия из-за моргания.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan BlinkEndTime;

    /// <summary>
    /// Дополнительное время между морганиями.
    /// Может быть установлено какой-нибудь системой в качестве усиления персонажа
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan AdditionalBlinkingTime;

    /// <summary>
    /// Прототип алерта слева от чата.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BlinkingAlert = "Blinking";

    #region Eye closing

    /// <summary>
    /// Закрыты ли глаза
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EyesState State = EyesState.Opened;

    /// <summary>
    /// Закрыты ли глаза вручную?
    /// Обозначает, что следующее открытие глаз требуется сделать так же вручную.
    /// </summary>
    /// TODO: Переименовать в ManuallyStateChanged
    [ViewVariables, AutoNetworkedField]
    public bool ManuallyClosed;

    /// <summary>
    /// Нужно ли показывать игроку эффекты при следующем открытии глаз.
    /// Используется, когда глаза игрока были закрыты через код
    /// </summary>
    /// TODO: Переименовать в ForceStateChanged
    [ViewVariables, AutoNetworkedField]
    public bool NextOpenEyesRequiresEffects;

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
[Serializable, NetSerializable]
public enum EyesState : byte
{
    Closed = 0,
    Opened = 1,
}

/// <summary>
/// Компонент, позволяющий видеть иконки рядом с персонажами, если у них закрыты глаза.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowBlinkableComponent : Component;
