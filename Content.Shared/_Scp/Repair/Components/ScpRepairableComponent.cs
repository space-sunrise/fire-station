using Content.Shared.Construction.Prototypes;
using Content.Shared.Random.Rules;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Repair.Components;

/// <summary>
/// Компонент, позволяющий сущности быть отремонтированной с использованием ConstructionGraph.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScpRepairableComponent : Component
{
    /// <summary>
    /// Граф, по которому будет производиться ремонт.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ConstructionGraphPrototype> Graph;

    /// <summary>
    /// Правила проверки, которые будут применяться к цели ремонта
    /// Если проверок нет или они проходят - ремонт доступен, иначе показывается сообщение о провале.
    /// </summary>
    [DataField]
    public ProtoId<RulesPrototype>? TargetRules;

    /// <summary>
    /// Правила проверки, которые будут применяться к использующему ремонт (игроку).
    /// Если проверок нет или они проходят - ремонт доступен, иначе показывается сообщение о провале.
    /// </summary>
    [DataField]
    public ProtoId<RulesPrototype>? UserRules;

    /// <summary>
    /// Белый список для ремонтируемой сущности (цели).
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    /// Черный список для ремонтируемой сущности (цели).
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;

    /// <summary>
    /// Белый список для использующего ремонт (игрока).
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    /// Черный список для использующего ремонт (игрока).
    /// </summary>
    [DataField]
    public EntityWhitelist? UserBlacklist;

    /// <summary>
    /// Строка, которая будет показываться определенным цветом, когда сущность можно отремонтировать.
    /// </summary>
    [DataField]
    public string ExamineMessage = "scp-repairable-can-be-repaired";

    /// <summary>
    /// Цвет, который будет использован для строки ExamineMessage.
    /// </summary>
    [DataField]
    public Color ExamineColor = Color.LightGray;

    #region Repair State

    /// <summary>
    /// Текущий узел в графе ремонта.
    /// </summary>
    [ViewVariables]
    public string? CurrentNode;

    /// <summary>
    /// Индекс текущего ребра в узле.
    /// </summary>
    [ViewVariables]
    public int? EdgeIndex;

    /// <summary>
    /// Индекс текущего шага в ребре.
    /// </summary>
    [ViewVariables]
    public int StepIndex;

    /// <summary>
    /// Стартовый узел графа (для возврата к началу цикла).
    /// </summary>
    [ViewVariables]
    public string? StartNode;

    #endregion
}

