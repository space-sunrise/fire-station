using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Backrooms;

[RegisterComponent, NetworkedComponent]
public sealed partial class AddComponentsPickupComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
