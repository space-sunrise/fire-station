using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveFearFallOffComponent : Component
{
    /// <summary>
    /// Шанс упасть при хождении во время страха постигшего <see cref="FearComponent.FallOffRequiredState"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FallOffChance = 0.03f;

    /// <summary>
    /// Время между проверками на возможность запнуться.
    /// </summary>
    [DataField]
    public TimeSpan FallOffCheckInterval = TimeSpan.FromSeconds(0.3f);

    /// <summary>
    /// Время следующей проверки на возможность запнуться при высоком уровне страха.
    /// </summary>
    [AutoNetworkedField, ViewVariables, AutoPausedField]
    public TimeSpan? FallOffNextCheckTime;

    [DataField]
    public TimeSpan FallOffTime = TimeSpan.FromSeconds(0.5f);
}
