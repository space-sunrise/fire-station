using Content.Shared._Scp.Mobs.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp082;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp082Component : ScpComponent
{
    [DataField]
    public TimeSpan NextHoldableAttemptDelay = TimeSpan.FromSeconds(30);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan? NextHoldableAttempt;
}