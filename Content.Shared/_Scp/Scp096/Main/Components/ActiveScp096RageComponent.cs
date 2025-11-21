using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActiveScp096RageComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan? RageStartTime;

    [DataField]
    public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/scream.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithLoop(true));
}
