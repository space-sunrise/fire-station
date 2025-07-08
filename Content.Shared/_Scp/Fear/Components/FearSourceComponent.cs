using Content.Shared._Scp.Helpers;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear.Components;

/// <summary>
/// Сущность с этим компонентом будет являться источников страха для игрока.
/// Здесь настраивается, какой уровень страха будет вызван у игрока при разных обстоятельствах.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FearSourceComponent : Component
{
    /// <summary>
    /// Какой уровень страха будет у жертвы, когда она увидит это?
    /// </summary>
    [DataField, ViewVariables]
    public FearState UponSeenState = FearState.Anxiety;

    /// <summary>
    /// Какой уровень страха будет у жертвы, когда она подойдет близко?
    /// </summary>
    [DataField, ViewVariables]
    public FearState UponComeCloser = FearState.Fear;

    /// <summary>
    /// Сила шейдера зернистости при приближении.
    /// Минимальное значение отображает силу при минимальном приближении.
    /// Максимальное при максимальном.
    /// </summary>
    [DataField, ViewVariables]
    public MinMaxExtended GrainShaderStrength = new (0, 800);

    /// <summary>
    /// Сила шейдера виньетки при приближении.
    /// Минимальное значение отображает силу при минимальном приближении.
    /// Максимальное при максимальном.
    /// </summary>
    [DataField, ViewVariables]
    public MinMaxExtended VignetteShaderStrength = new (0, 300);
}
