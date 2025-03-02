namespace Content.Server._Scp.Research.Artifacts.Effects.ModifyHunger;

[RegisterComponent]
public sealed partial class ArtifactModifyHungerComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
