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
    [DataField, AutoNetworkedField, ViewVariables]
    public float CloseRange = 5f;

    /// <inheritdoc cref="LineOfSightBlockerLevel"/>
    [DataField, AutoNetworkedField, ViewVariables]
    public LineOfSightBlockerLevel RequiredLineOfSight  = LineOfSightBlockerLevel.None;
}

/// <summary>
/// Уровень необходимой прямой видимости.
/// Определяет, насколько допустимы преграды между сущностями.
/// </summary>
/// <remarks>
/// Это создано, чтобы как-то единым образом обозначить и назвать "когда между этим и тем стоит стена/окно/ничего нет"
/// </remarks>
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
/// <param name="target">Цель, которая приблизилась</param>
/// <param name="range">Текущее расстояние сущности до цели</param>
/// <param name="closeRange">Расстояние, на котором начинается триггер ивента</param>
/// <param name="type">Фактический уровень видимости</param>
public sealed class ProximityInRangeReceiverEvent(EntityUid target, float range, float closeRange, LineOfSightBlockerLevel type) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly float Range = range;
    public readonly float CloseRange = closeRange;
    public readonly LineOfSightBlockerLevel Type = type;
}

/// <summary>
/// Ивент, вызываемый при приближении <see cref="ProximityTargetComponent"/> к <see cref="ProximityReceiverComponent"/>.
/// Вызывается на цели, которая приблизилась.
/// </summary>
/// <param name="receiver">Сущность, к которой приблизились</param>
/// <param name="range">Текущее расстояние цели до сущности</param>
/// <param name="closeRange">Расстояние, на котором начинается триггер ивента</param>
/// <param name="type">Фактический уровень видимости</param>
public sealed class ProximityInRangeTargetEvent(EntityUid receiver, float range, float closeRange, LineOfSightBlockerLevel type) : EntityEventArgs
{
    public readonly EntityUid Receiver = receiver;
    public readonly float Range = range;
    public readonly float CloseRange = closeRange;
    public readonly LineOfSightBlockerLevel Type = type;
}

/// <summary>
/// Ивент, вызываемый, когда сущность <see cref="ProximityTargetComponent"/> отсутствует рядом с любым <see cref="ProximityReceiverComponent"/>.
/// Вызывается на цели.
/// Служит, чтобы убирать какие-то эффекты, вызванные ивента приближения.
/// </summary>
[ByRefEvent]
public record struct ProximityNotInRangeTargetEvent;
