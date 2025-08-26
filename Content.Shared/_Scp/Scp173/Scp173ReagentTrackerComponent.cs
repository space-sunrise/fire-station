using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp173;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp173ReagentTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentReagentAmount;

    [DataField, AutoNetworkedField]
    public bool IsInContainment;
}
