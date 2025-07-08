using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FearActiveSoundEffectsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public float AdditionalVolume;

    #region HeartBeat

    [ViewVariables, AutoNetworkedField]
    public float Pitch = 1f;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextHeartbeatCooldown = TimeSpan.FromSeconds(SharedFearSystem.HeartBeatMinimumCooldown);

    [ViewVariables]
    public TimeSpan? NextHeartbeatTime;

    #endregion

    #region Breathing

    [AutoNetworkedField, ViewVariables]
    public EntityUid? BreathingAudioStream;

    #endregion

}
