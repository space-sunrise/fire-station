using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Components.Fears;

/// <summary>
/// Компонент, отвечающий за страх перед кровью
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HemophobiaComponent : Component
{
    /// <summary>
    /// Прототип реагента крови, на который будет проверка.
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<ReagentPrototype> Reagent = "Blood";

    /// <summary>
    /// Какое количество окружающей крови будет пугать персонажа?
    /// </summary>
    [DataField, ViewVariables]
    public FixedPoint2 ScaryBloodAmount = 25f;

    /// <summary>
    /// Какой уровень страха будет установлен при виде крови?
    /// </summary>
    [DataField, ViewVariables]
    public FearState UponSeenBloodState = FearState.Fear;
}
