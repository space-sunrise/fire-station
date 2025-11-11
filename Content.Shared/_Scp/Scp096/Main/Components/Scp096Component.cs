using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class Scp096Component : Component
{
    [DataField]
    public float ArgoAngle = 25f;

    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromSeconds(240f); // 4 минуты

    [DataField]
    public TimeSpan PacifiedTime = TimeSpan.FromSeconds(60f);

    [DataField]
    public TimeSpan RageHeatUp = TimeSpan.FromSeconds(30f);

    [DataField]
    public float BaseSpeed = 1.5f;

    [DataField]
    public float RageSpeed = 8f;

    /// <summary>
    /// Какие предметы скромник сможет поднимать?
    /// </summary>
    [DataField]
    public EntityWhitelist? PickupWhitelist;

    /// <summary>
    /// Какие предметы скромник НЕ сможет поднимать?
    /// </summary>
    [DataField]
    public EntityWhitelist? PickupBlacklist;

    #region Sounds

    [DataField]
    public SoundSpecifier CrySound = new SoundPathSpecifier("/Audio/_Scp/Scp096/crying.ogg",
        AudioParams.Default.WithVolume(-14f).WithMaxDistance(4f).WithLoop(true));

    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg");

    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/triggered.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f));

    [ViewVariables, AutoNetworkedField, NonSerialized]
    public EntityUid? AudioStream;

    #endregion

    #region Animations

    [DataField]
    public TimeSpan AnimationDuration = TimeSpan.FromSeconds(2f);

    [ViewVariables, AutoNetworkedField]
    public bool AgroToDeadAnimation;

    [ViewVariables, AutoNetworkedField]
    public bool DeadToIdleAnimation;

    #endregion
}
