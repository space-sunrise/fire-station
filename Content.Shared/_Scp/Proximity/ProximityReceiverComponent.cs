using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Proximity;

/// <summary>
/// Компонент-маркер, обозначающий что-то, к чему будут приближаться.
/// При приближении будет вызван ивент
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProximityReceiverComponent : Component
{
    /// <summary>
    /// На каком расстоянии будут вызываться ивент?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CloseRange = 3f;

    /// <inheritdoc cref="LineOfSightBlockerLevel"/>
    [DataField, AutoNetworkedField]
    public LineOfSightBlockerLevel RequiredLineOfSight = LineOfSightBlockerLevel.Transparent;

    [DataField]
    public LookupFlags Flags = LookupFlags.Uncontained | LookupFlags.Approximate;
}

/// <summary>
/// Уровень необходимой прямой видимости.
/// Определяет, насколько допустимы преграды между сущностями.
/// </summary>
/// <remarks>
/// Это создано, чтобы как-то единым образом обозначить и назвать "когда между этим и тем стоит стена/окно/ничего нет"
/// </remarks>
/// TODO: Когда-нибудь это должно переехать в систему FOV.
public enum LineOfSightBlockerLevel
{
    /// <summary>
    /// Видимость должна быть полностью свободной. Любая преграда исключает активацию.
    /// То есть ничего не должно мешать.
    /// </summary>
    None,
    /// <summary>
    /// Видимость преграждается прозрачной сущностью.
    /// Например, окно.
    /// </summary>
    Transparent,
    /// <summary>
    /// Видимость должна быть полностью заблокирована.
    /// Например, стены.
    /// </summary>
    Solid,
}

/// <summary>
/// Ивент, вызываемый при приближении <see cref="ProximityTargetComponent"/> к <see cref="ProximityReceiverComponent"/>.
/// Вызывается на сущности, к которой приблизились.
/// </summary>
/// <param name="Target">Цель, которая приблизилась</param>
/// <param name="Range">Текущее расстояние сущности до цели</param>
/// <param name="CloseRange">Расстояние, на котором начинается триггер ивента</param>
/// <param name="Type">Фактический уровень видимости</param>
[ByRefEvent]
public readonly record struct ProximityInRangeReceiverEvent(EntityUid Target,
    float Range,
    float CloseRange,
    LineOfSightBlockerLevel Type);

/// <summary>
/// Ивент, вызываемый при приближении <see cref="ProximityTargetComponent"/> к <see cref="ProximityReceiverComponent"/>.
/// Вызывается на цели, которая приблизилась.
/// </summary>
/// <param name="Receiver">Сущность, к которой приблизились</param>
/// <param name="Range">Текущее расстояние цели до сущности</param>
/// <param name="CloseRange">Расстояние, на котором начинается триггер ивента</param>
/// <param name="Type">Фактический уровень видимости</param>
[ByRefEvent]
public readonly record struct ProximityInRangeTargetEvent(EntityUid Receiver,
    float Range,
    float CloseRange,
    LineOfSightBlockerLevel Type);

/// <summary>
/// Ивент, вызываемый, когда сущность <see cref="ProximityTargetComponent"/> отсутствует рядом с любым <see cref="ProximityReceiverComponent"/>.
/// Вызывается на цели.
/// Служит, чтобы убирать какие-то эффекты, вызванные ивента приближения.
/// </summary>
[ByRefEvent]
public readonly record struct ProximityNotInRangeTargetEvent;

/// <summary>
/// Ивент, вызываемый на цели, когда рядом впервые появляется dominant proximity receiver.
/// </summary>
[ByRefEvent]
public readonly record struct ProximityTargetEnteredEvent(
    EntityUid Receiver,
    float Range,
    float CloseRange,
    LineOfSightBlockerLevel Type);

/// <summary>
/// Ивент, вызываемый на цели, когда она перестает находиться рядом с dominant proximity receiver.
/// </summary>
[ByRefEvent]
public readonly record struct ProximityTargetExitedEvent(EntityUid Receiver);

/// <summary>
/// Ивент, вызываемый на цели, когда ее dominant proximity receiver сменился.
/// </summary>
[ByRefEvent]
public readonly record struct ProximityTargetReceiverChangedEvent(
    EntityUid OldReceiver,
    EntityUid NewReceiver,
    float Range,
    float CloseRange,
    LineOfSightBlockerLevel Type);
