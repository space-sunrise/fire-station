using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpHoldRestrictedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ScpHoldStage Stage = ScpHoldStage.Full;
}
