namespace Content.Server._Scp.Research.Artifacts.Effects.ModifyThirst;

[RegisterComponent]
public sealed partial class ArtifactModifyThirstComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
