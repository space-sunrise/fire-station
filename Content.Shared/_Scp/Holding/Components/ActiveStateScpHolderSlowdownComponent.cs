using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Runtime slowdown state stored on an active holder while their movement is penalized.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ActiveStateScpHolderSlowdownComponent : Component
{
    /// <summary>
    /// Walk speed modifier applied while the holder contributes to an active hold.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public float WalkModifier = 1f;

    /// <summary>
    /// Sprint speed modifier applied while the holder contributes to an active hold.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public float SprintModifier = 1f;
}
