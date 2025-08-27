using Content.Shared._Scp.Proximity;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Containment.Cage;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpCageComponent : Component
{
    /// <summary>
    /// Если рядом будут сущности подходящие под данный список, то EntityStorage не сможет закрыться
    /// </summary>
    [DataField]
    public EntityWhitelist? CloseStorageBlacklist;

    /// <summary>
    /// Если рядом будут сущности подходящие под данный список, то EntityStorage не сможет открыться
    /// </summary>
    [DataField]
    public EntityWhitelist? OpenStorageBlacklist;

    /// <summary>
    /// Реагент, который будет предотвращать взаимодействие с дверцей EntityStorage
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? StopReagent;

    /// <summary>
    /// Необходимое количество реагента, которое будет предотвращать взаимодействие с дверцей EntityStorage
    /// </summary>
    [DataField]
    public FixedPoint2 StopReagentVolume;

    /// <summary>
    /// Радиус поиска объектов при проверке на черный список.
    /// </summary>
    [DataField]
    public float SearchRadius = 5f;

    /// <summary>
    /// Требуемый уровень видимости объекта. Подробнее про LineOfSightBlockerLevel:
    /// <see cref="LineOfSightBlockerLevel"/>
    /// </summary>
    [DataField]
    public LineOfSightBlockerLevel LineOfSight = LineOfSightBlockerLevel.None;

    /// <summary>
    /// Причина, которую покажут игроку, если система не даст взаимодействовать с дверцей.
    /// </summary>
    [DataField]
    public string? Reason;
}
