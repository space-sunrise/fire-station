using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class Scp096Component : Component
{
    [AutoNetworkedField, ViewVariables]
    public bool InRageMode;

    [AutoNetworkedField, ViewVariables]
    public HashSet<EntityUid> Targets = [];

    [DataField]
    public float AgroDistance = 10f;

    [DataField]
    public float ArgoAngle = 25;

    [AutoNetworkedField]
    public TimeSpan? RageStartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromSeconds(180f);

    [DataField]
    public TimeSpan PacifiedTime = TimeSpan.FromSeconds(60f);

    [DataField]
    public TimeSpan RageHeatUp = TimeSpan.FromSeconds(30f);

    [DataField]
    public float WireCutChance = 0.4f;

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
    public SoundSpecifier DoorSmashSoundCollection =
        new SoundCollectionSpecifier("MetalSlam", AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

    [DataField]
    public SoundSpecifier CrySound = new SoundPathSpecifier("/Audio/_Scp/Scp096/crying.ogg");

    [DataField]
    public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/scream.ogg");

    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg");

    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/triggered.ogg");

    #endregion
}
