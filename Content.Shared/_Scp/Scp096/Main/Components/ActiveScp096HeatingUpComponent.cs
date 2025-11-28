using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ActiveScp096HeatingUpComponent : Component
{
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? RageHeatUpEnd;

    [DataField]
    public TimeSpan RageHeatUp = TimeSpan.FromSeconds(30f);

    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/triggered.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithRolloffFactor(5f));
}
