using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Components.Traits;

/// <summary>
/// Компонент, вызывающий обморочное состояние при определенном уровне страха.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FearFaintingComponent : Component
{
    /// <summary>
    /// Требуемый уровень страха для падения в обморок
    /// </summary>
    [DataField, ViewVariables]
    public FearState RequiredState = FearState.Fear;

    /// <summary>
    /// Время, которое персонаж проведет в обмороке
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan Time = TimeSpan.FromSeconds(20f);

    /// <summary>
    /// Шанс упасть в обморок при достижении <see cref="RequiredState"/>
    /// </summary>
    [DataField, ViewVariables]
    public float Chance = 30f;

    public static readonly EntProtoId StatusEffect = "StatusEffectForcedSleeping";
}
