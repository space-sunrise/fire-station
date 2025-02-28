using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Research.Artifacts.Effects.AddComponentsInRadius;

[RegisterComponent]
public sealed partial class AddComponentsInRadiusComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField, ViewVariables]
    public float Radius = 12f;
}
