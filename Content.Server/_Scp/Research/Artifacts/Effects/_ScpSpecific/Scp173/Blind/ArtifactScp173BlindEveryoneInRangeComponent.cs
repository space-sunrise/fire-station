namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp173.Blind;

[RegisterComponent]
public sealed partial class ArtifactScp173BlindEveryoneInRangeComponent : Component
{
    [DataField] public TimeSpan Time = TimeSpan.FromSeconds(8);
}
