namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp173.Blind;

[RegisterComponent]
public sealed partial class ArtifactScp173BlindEveryoneInRangeComponent : Component
{
    [DataField] public float Radius = 12f;
    [DataField] public float Time = 8f;
}
