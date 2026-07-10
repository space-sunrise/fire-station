using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Prevents the target from being held again for a short period after a successful full breakout.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ScpHoldImmuneComponent : Component
{
    /// <summary>
    /// Timestamp when the immunity expires.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;
}
