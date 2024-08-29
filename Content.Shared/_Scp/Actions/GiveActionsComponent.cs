using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Actions;

/// <summary>
/// Helper component for giving hundreds of actions for scp
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GiveActionsComponent : Component
{
    /// <summary>
    /// List with actions entity prototype ids
    /// </summary>
    [DataField(required: true)]
    public HashSet<EntProtoId> Actions = default!;
}
