namespace Content.Server._Scp.Research.Artifacts.Effects._ScpSpecific.Scp106.Teleport;

[RegisterComponent]
public sealed partial class ArtifactScp106TeleportComponent : Component
{
    [DataField]
    public ArtifactScp106TeleportMode Mode = ArtifactScp106TeleportMode.Random;
}

public enum ArtifactScp106TeleportMode : byte
{
    Random,
    Station,
    Dimension,
}
