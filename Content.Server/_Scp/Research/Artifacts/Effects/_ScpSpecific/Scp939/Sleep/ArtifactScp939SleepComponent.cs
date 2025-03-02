namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp939.Sleep;

[RegisterComponent]
public sealed partial class ArtifactScp939SleepComponent : Component
{
    [DataField]
    public float MinSleepTime = 20f;

    [DataField]
    public float MaxSleepTime = 80f;
}
