using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp330;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp330CandyComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? TackedBy;
}
