namespace Content.Server._Scp.Research.Artifacts.Effects.Thirst;

[RegisterComponent]
public sealed partial class ThirstArtifactComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
