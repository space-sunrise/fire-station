using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Marks a victim hand placeholder virtual item created by SCP holding.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ScpHeldHandBlockerComponent : Component
{
}
