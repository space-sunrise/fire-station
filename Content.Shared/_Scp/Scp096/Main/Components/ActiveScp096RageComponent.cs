using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActiveScp096RageComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromMinutes(4f);

    [DataField]
    public TimeSpan PacifiedTime = TimeSpan.FromSeconds(60f);

    [AutoNetworkedField]
    public TimeSpan? RageStartTime;

    [DataField]
    public float Speed = 8f;

    [DataField]
    public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/scream.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithRolloffFactor(5f).WithLoop(true));
}
