using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Components.Fears;

/// <summary>
/// Компонент, отвечающий за страх перед кровью
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HemophobiaComponent : Component
{
    /// <summary>
    /// Прототип реагента крови, на который будет проверка.
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<ReagentPrototype> Reagent = "Blood";

    /// <summary>
    /// Какой уровень страха будет установлен при виде крови?
    /// </summary>
    [DataField, ViewVariables]
    public FearState UponSeenBloodState = FearState.Fear;

    /// <summary>
    /// Определяет пороговые значения количества крови рядом, которые приведут к повышению уровня страха.
    /// </summary>
    [DataField, ViewVariables]
    public Dictionary<FearState, FixedPoint2> BloodRequiredPerState = new()
    {
        { FearState.None, 0f },
        { FearState.Anxiety, 20f },
        { FearState.Fear, 35f },
        { FearState.Terror, 80f },
    };

    [AutoNetworkedField, ViewVariables]
    public List<KeyValuePair<FearState, FixedPoint2>> SortedBloodRequiredPerState = [];

    /// <summary>
    /// Айди прототипа фобии для гемофобии
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<PhobiaPrototype> Phobia = "Hemophobia";
}
