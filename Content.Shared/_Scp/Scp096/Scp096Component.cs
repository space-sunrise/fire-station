using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Scp096;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096Component : Component
{
    [AutoNetworkedField]
    public bool InRageMode { get; set; } = false;

    [AutoNetworkedField]
    public bool Pacified { get; set; } = false;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> Targets { get; set; } = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AgroDistance = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ArgoAngle = 25;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? RageStartTime { get; set; }

    [DataField, AutoNetworkedField , ViewVariables(VVAccess.ReadWrite)]
    public float RageDuration { get; set; }= 2000f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PacifiedTime = 60f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WireCutChance = 0.4f;

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
}
