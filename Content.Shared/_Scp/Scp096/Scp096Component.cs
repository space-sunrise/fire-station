using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096Component : Component
{
    [AutoNetworkedField]
    public bool InRageMode;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> Targets = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AgroDistance = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ArgoAngle = 25;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? RageStartTime;

    [DataField, AutoNetworkedField , ViewVariables(VVAccess.ReadWrite)]
    public float RageDuration = 180f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PacifiedTime = 60f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WireCutChance = 0.4f;

    #region Sounds

    [DataField]
    public SoundSpecifier DoorSmashSoundCollection = new SoundCollectionSpecifier("MetalSlam")
    {
        Params = new AudioParams()
        {
            Variation = 0.2f,
            Volume = -2f,
        }
    };

    public SoundSpecifier CrySound { get; } = new SoundPathSpecifier("/Audio/_Scp/Scp096/scp-096-crying.ogg");

    public SoundSpecifier RageSound { get; } = new SoundPathSpecifier("/Audio/_Scp/Scp096/scp-096-scream.ogg");

    #endregion

    #region Speed

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseSpeed = 1.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RageSpeed = 8f;

    #endregion


}
