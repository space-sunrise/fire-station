namespace Content.Server._Scp.Research.Artifacts.Effects.BoltAirlocks;

[RegisterComponent]
public sealed partial class ArtifactBoltAirlocksComponent : Component
{
    [DataField]
    public float Range = 12f;

    [DataField]
    public float Chance = 70f;
}
