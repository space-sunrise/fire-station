using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp330;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp330CandyComponent : Component
{
    [ViewVariables]
    public EntityUid? TakenBy;

    [DataField]
    public FixedPoint2 ReagentQuantity = 10;

    [DataField]
    public string SolutionName = "food";
}
