using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActiveScp096RageComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan? RageStartTime;

    [DataField]
    public float WireCutChance = 0.4f;

    [DataField]
    public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/scream.ogg");

    [DataField]
    public SoundSpecifier DoorSmashSoundCollection =
        new SoundCollectionSpecifier("MetalSlam", AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));
}
