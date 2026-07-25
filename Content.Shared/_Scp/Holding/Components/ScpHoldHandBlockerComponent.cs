using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Marks a virtual item that reserves one holder hand for an active SCP hold.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ScpHoldHandBlockerComponent : Component
{
}
