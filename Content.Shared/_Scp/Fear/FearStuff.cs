using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Fear;

/// <summary>
/// Уровни страха. Чем больше значение, тем сильнее страх
/// </summary>
/// TODO: Возможно сделать struct, отвечающий за страх и параметры
[Serializable, NetSerializable]
public enum FearState : byte
{
    /// <summary>
    /// Отсутствие страха. Сущность в спокойном состоянии
    /// </summary>
    None = 0,

    /// <summary>
    /// Тревожность. Сущность немного напугана
    /// </summary>
    Anxiety = 1,

    /// <summary>
    /// Страх. Сущность испытает прямой страх от чего-либо
    /// </summary>
    Fear = 2,

    /// <summary>
    /// Неконтролируемый ужас. Сущность невероятно напугана, что-то СЛИШКОМ ужасно, чтобы знать об этом.
    /// </summary>
    Terror = 3,
}

/// <summary>
/// Ивент, вызывающийся при попытке сущности успокоиться.
/// Используется для отмены успокоения
/// </summary>
/// <param name="newState">Уровень страха, который будет установлен при успешном успокоении</param>
public sealed class FearCalmDownAttemptEvent(FearState newState) : CancellableEntityEventArgs
{
    public readonly FearState NewState = newState;
}

/// <summary>
/// Ивент, вызывающийся при изменении уровня страха.
/// </summary>
/// <param name="NewState">Новый уровень страха, который был установлен</param>
/// <param name="OldState">Старый уровень страха, который сменили</param>
[ByRefEvent]
public record struct FearStateChangedEvent(FearState NewState, FearState OldState)
{
    public readonly FearState NewState = NewState;
    public readonly FearState OldState = OldState;
}

/// <summary>
/// Прототип фобии, содержащий информацию о ней.
/// </summary>
[Prototype]
public sealed partial class PhobiaPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Description = string.Empty;

    [DataField]
    public Color? Color;
}
