using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Runtime state stored on a target while at least one holder is contributing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true), AutoGenerateComponentPause]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ActiveScpHoldableComponent : Component
{
    /// <summary>
    /// Next timestamp when a soft breakout attempt may succeed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SoftEscapeAvailableAt;

    /// <summary>
    /// Ordered holder list used for contribution counting and per-holder runtime coordination.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public List<EntityUid> Holders = [];

    /// <summary>
    /// Required contributor count for entering full hold.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public int RequiredHolderCount = 2;
}
