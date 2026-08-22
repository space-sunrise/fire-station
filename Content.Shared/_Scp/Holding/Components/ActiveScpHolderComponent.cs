using Content.Shared._Scp.Holding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Runtime contribution state stored on each active holder.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
[Access(typeof(SharedScpHoldingSystem))]
public sealed partial class ActiveScpHolderComponent : Component
{
    /// <summary>
    /// Target currently being contributed to.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public EntityUid? Target;

    /// <summary>
    /// Raw per-holder desired cursor target in world space.
    /// Each tick it is clamped relative to the holder's current position,
    /// so far clicks keep their direction even if the holder moves.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public EntityCoordinates CursorTargetCoordinates = EntityCoordinates.Invalid;

    /// <summary>
    /// True while this holder is still actively pulling the held target toward its stored cursor target.
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public bool CursorMoveActive;
}
