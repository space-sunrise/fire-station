namespace Content.Server._Scp.Research.Artifacts.Effects.Hunger;

[RegisterComponent]
public sealed partial class HungerArtifactComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
