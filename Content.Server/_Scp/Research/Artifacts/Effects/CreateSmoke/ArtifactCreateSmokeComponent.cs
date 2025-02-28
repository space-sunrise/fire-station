using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Research.Artifacts.Effects.CreateSmoke;

[RegisterComponent]
public sealed partial class ArtifactCreateSmokeComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Quantity;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Prototype;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 30.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SpreadRadius = 10;
}
