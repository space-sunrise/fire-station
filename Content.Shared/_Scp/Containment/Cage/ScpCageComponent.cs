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
    /// Если рядом будут сущности подходящие под данный лист, то EntityStorage не сможет закрыться
    /// </summary>
    [DataField]
    public EntityWhitelist? CloseStorageBlacklist;

    /// <summary>
    /// Если рядом будут сущности подходящие под данный лист, то EntityStorage не сможет открыться
    /// </summary>
    [DataField]
    public EntityWhitelist? OpenStorageBlacklist;

    [DataField]
    public ProtoId<ReagentPrototype>? StopReagent;

    [DataField]
    public FixedPoint2 StopReagentVolume;

    /// <summary>
    /// Радиус поиска объектов при проверке на блеклист.
    /// </summary>
    [DataField]
    public float SearchRadius = 5f;

    /// <summary>
    /// Требуемый уровень видимости объекта. Подробнее про LineOfSightBlockerLevel:
    /// <inheritdoc cref="LineOfSightBlockerLevel"/>
    /// </summary>
    [DataField]
    public LineOfSightBlockerLevel LineOfSight = LineOfSightBlockerLevel.None;

    [DataField]
    public string? Reason;
}
