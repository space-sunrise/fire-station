using Content.Shared._Scp.Helpers;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear.Components;

/// <summary>
/// Сущность с этим компонентом будет являться источников страха для игрока.
/// Здесь настраивается, какой уровень страха будет вызван у игрока при разных обстоятельствах.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FearSourceComponent : Component
{
    /// <summary>
    /// Какой уровень страха будет у жертвы, когда она увидит это?
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public FearState UponSeenState = FearState.Anxiety;

    /// <summary>
    /// Какой уровень страха будет у жертвы, когда она подойдет близко?
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public FearState UponComeCloser = FearState.Fear;

    /// <summary>
    /// Сила шейдера зернистости при приближении.
    /// Минимальное значение отображает силу при минимальном приближении.
    /// Максимальное при максимальном.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public MinMaxExtended GrainShaderStrength = new (0, 800);

    /// <summary>
    /// Сила шейдера виньетки при приближении.
    /// Минимальное значение отображает силу при минимальном приближении.
    /// Максимальное при максимальном.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public MinMaxExtended VignetteShaderStrength = new (0, 300);

    /// <summary>
    /// Должен ли источник страха вызывать звуки дыхания у пугающегося при приближении?
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool PlayBreathingSound = true;

    /// <summary>
    /// Должен ли источник страха вызывать звуки сердцебиения у пугающегося при приближении?
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool PlayHeartbeatSound = true;

    /// <summary>
    /// Вайтлист сущностей, которые могут испугаться этого источника страха.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public EntityWhitelist? FearWhitelist;
}
