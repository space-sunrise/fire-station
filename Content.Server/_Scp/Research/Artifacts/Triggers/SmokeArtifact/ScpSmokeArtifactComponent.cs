using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Research.Artifacts.Triggers.SmokeArtifact;

[RegisterComponent]
public sealed partial class ScpSmokeArtifactComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution SmokeSolution = new ("АМН-С227", 200);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SmokeDuration = 30.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SmokeSpreadRadius = 10;

    [DataField]
    public EntProtoId SmokeProtoId = "АМН-С227Smoke";
}
