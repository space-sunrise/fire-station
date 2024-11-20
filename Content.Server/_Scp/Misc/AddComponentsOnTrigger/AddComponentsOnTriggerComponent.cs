using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Misc.AddComponentsOnTrigger;

[RegisterComponent]
public sealed partial class AddComponentsOnTriggerComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
