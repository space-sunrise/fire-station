using Robust.Shared.GameStates;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChaosRaidShuttleComponent : Component
{
    [ViewVariables]
    public EntityUid? AssociatedRule;
}
