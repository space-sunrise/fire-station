using Content.Shared._Scp.Fear.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear.Components;

/// <summary>
/// Компонент, отвечающий за звуковые эффекты страха при приближении к источнику.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FearActiveSoundEffectsComponent : Component
{
    /// <summary>
    /// Дополнительная громкость, применяемая к звукам эффектам.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float AdditionalVolume;

    #region HeartBeat

    /// <summary>
    /// Должен ли звук сердцебиения проигрываться?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool PlayHeartbeatSound = true;


    /// <summary>
    /// Питч, который накладывается на звуки сердцебиения.
    /// Зависит от расстояния до источника страха
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Pitch = 1f;

    /// <summary>
    /// Время между ударами сердца.
    /// Уменьшается при приближении к источнику страха.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextHeartbeatCooldown = TimeSpan.FromSeconds(SharedFearSystem.HeartBeatMinimumCooldown);

    /// <summary>
    /// Следующее время удара сердца.
    /// </summary>
    [ViewVariables]
    public TimeSpan? NextHeartbeatTime;

    #endregion

    #region Breathing

    /// <summary>
    /// Должен ли звук дыхания проигрываться?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool PlayBreathingSound = true;

    /// <summary>
    /// <see cref="EntityUid"/> зацикленного звука дыхания
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public EntityUid? BreathingAudioStream;

    #endregion

}
