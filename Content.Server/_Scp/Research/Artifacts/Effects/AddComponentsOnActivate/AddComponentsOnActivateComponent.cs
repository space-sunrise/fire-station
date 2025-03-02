using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Research.Artifacts.Effects.AddComponentsOnActivate;

[RegisterComponent]
public sealed partial class AddComponentsOnActivateComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
