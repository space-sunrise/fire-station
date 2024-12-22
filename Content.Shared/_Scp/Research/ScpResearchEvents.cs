using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Research;

#region Base

[Serializable, NetSerializable]
public abstract partial class BaseScpInteractDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Исследовательский инструмент, которым взаимодействовали с СЦП
    /// </summary>
    [NonSerialized]
    public EntityUid Tool;

    public BaseScpInteractDoAfterEvent() {}

    public BaseScpInteractDoAfterEvent(EntityUid tool)
    {
        Tool = tool;
    }
}

/// <summary>
/// Ивент, вызываемый дуафтером, когда игрок взаимодействует с СЦП каким-то предметом для сбора исследовательского материала
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ScpSpawnInteractDoAfterEvent : BaseScpInteractDoAfterEvent
{
    /// <summary>
    /// Исследовательский материал, который будет получен в результате взаимодействия с СЦП.
    /// </summary>
    [DataField]
    public EntProtoId ToSpawn;

    /// <summary>
    /// Максимальное количество материала, который будет заспавнен
    /// </summary>
    [DataField]
    public int MinSpawn = 1;

    /// <summary>
    /// Минимальное количество материала, который будет заспавнен
    /// </summary>
    [DataField]
    public int MaxSpawn = 1;

    public ScpSpawnInteractDoAfterEvent() {}

    public ScpSpawnInteractDoAfterEvent(EntProtoId toSpawn, int minSpawn, int maxSpawn)
    {
        ToSpawn = toSpawn;
        MinSpawn = minSpawn;
        MaxSpawn = maxSpawn;
    }
}

#endregion

