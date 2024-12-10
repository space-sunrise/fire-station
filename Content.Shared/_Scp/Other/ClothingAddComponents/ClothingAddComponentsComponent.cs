using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ClothingAddComponents;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingAddComponentsComponent : Component
{
    [DataField(required:true)]
    public ComponentRegistry Components = new();
}
