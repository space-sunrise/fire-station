using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Damage.ExaminableDamage;

/// <summary>
/// Компонент для отображения информации о повреждениях при осмотре сущностей.
/// </summary>
/// <seealso cref="SharedScpExaminableDamageSystem"/>
/// <remarks>
/// Поддерживает различные сущности, как живые сущности, так и структуры.
/// Тип расчета повреждений зависит от <see cref="Mode"/>
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScpExaminableDamageComponent : Component
{
    /// <summary>
    /// Список сообщений о повреждениях.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? GeneralMessages;

    /// <summary>
    /// Цвет сообщений о повреждениях
    /// </summary>
    [DataField]
    public Color Color = Color.Gray;

    /// <summary>
    /// Режим расчета повреждений (для структур, мобов до критического состояния или до смерти).
    /// </summary>
    [DataField]
    public ScpExaminableDamageMode Mode = ScpExaminableDamageMode.MobToDeath;

    /// <summary>
    /// Дополнительная информация, которую будет видеть игрок, обладая определенным департаментом.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DepartmentPrototype>, ProtoId<LocalizedDatasetPrototype>> DepartmentMessages = new();

    /// <summary>
    /// Дополнительная информация, которую будет видеть игрок, обладая определенной работой.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<LocalizedDatasetPrototype>> JobMessages = new();
}

/// <summary>
/// Режим расчета повреждений для сущности.
/// От него зависит метод получения максимального доступного урона.
/// </summary>
public enum ScpExaminableDamageMode : byte
{
    /// <summary>
    /// Режим расчета для структур, использующих <see cref="DestructibleComponent"/> для получения максимального урона до разрушения.
    /// </summary>
    Structure,

    /// <summary>
    /// Режим расчета для живых сущностей, использующий <see cref="MobState.Critical"/> состояние как порог максимального урона
    /// </summary>
    MobToCritical,

    /// <summary>
    /// Режим расчета для живых сущностей, использующий <see cref="MobState.Dead"/> состояние как порог максимального урона
    /// </summary>
    MobToDeath,
}
