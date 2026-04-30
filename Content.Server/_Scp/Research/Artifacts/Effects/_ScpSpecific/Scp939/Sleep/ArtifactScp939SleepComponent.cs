namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp939.Sleep;

[RegisterComponent]
public sealed partial class ArtifactScp939SleepComponent : Component
{
    [DataField]
    public TimeSpan MinSleepTime = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan MaxSleepTime = TimeSpan.FromSeconds(80);
}
