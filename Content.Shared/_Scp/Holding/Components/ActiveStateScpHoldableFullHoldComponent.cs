using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Runtime full-hold state stored on a target while it is immobilized.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ActiveStateScpHoldableFullHoldComponent : Component
{
    /// <summary>
    /// Timestamp when the current uninterrupted full hold started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan StartedAt;
}
