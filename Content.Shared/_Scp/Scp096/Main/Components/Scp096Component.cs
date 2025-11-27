using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class Scp096Component : Component
{
    [DataField]
    public float ArgoAngle = 25f;

    // TODO: Перенести это в свои компоненты, че это тут делает.
    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromSeconds(240f); // 4 минуты

    [DataField]
    public TimeSpan PacifiedTime = TimeSpan.FromSeconds(60f);

    [DataField]
    public TimeSpan RageHeatUp = TimeSpan.FromSeconds(30f);

    [DataField]
    public float Speed = 1.5f;

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
        AudioParams.Default.WithVolume(-14f).WithMaxDistance(4f).WithRolloffFactor(5f).WithLoop(true));

    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg");

    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/triggered.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithRolloffFactor(5f));

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

    #region CryOut

    [DataField]
    public DamageSpecifier CryOutDamage = new ()
    {
        DamageDict = new()
        {
            { "Structural", 50 },
        },
    };

    [DataField]
    public float CryOutRange = 6f;

    [DataField]
    public EntityWhitelist CryOutWhitelist = new ()
    {
        Tags = new List<ProtoId<TagPrototype>>
        {
            "Wall",
            "Window",
            "Directional",
        },
    };

    [DataField]
    public SoundSpecifier CryOutSound = new SoundCollectionSpecifier("IdleScp096",
        AudioParams.Default.WithMaxDistance(6f));

    [DataField]
    public TimeSpan CryOutJitterTime = TimeSpan.FromSeconds(1f);

    #endregion

    #region Face skin rip

    [DataField]
    public TimeSpan FaceSkinRipDoAfterTime = TimeSpan.FromSeconds(5f);

    [DataField]
    public DamageSpecifier FaceSkinRipDamageToFace = new()
    {
        DamageDict = new()
        {
            { "Blunt", 20 },
        },
    };

    [DataField]
    public SoundSpecifier? FaceSkinRipDamageToFaceSound = new SoundCollectionSpecifier("FaceSkinRip");

    [DataField]
    public EntProtoId FaceProto = "Scp096Face";

    [DataField]
    public string FaceContainer = "face_slot";

    [ViewVariables, AutoNetworkedField, NonSerialized]
    public EntityUid? FaceEntity;

    #endregion
}
