using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Misc.ClothingAddComponents;

[RegisterComponent]
public sealed partial class ClothingAddComponentsComponent : Component
{
    [DataField(required:true)]
    public ComponentRegistry Components = new();
}
