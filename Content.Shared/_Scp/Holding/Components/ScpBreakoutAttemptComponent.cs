using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Semantic state that marks an active breakout attempt during a full hold.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ScpBreakoutAttemptComponent : Component;
