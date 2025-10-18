using Content.Shared._Scp.ComplexElevator;

namespace Content.Client._Scp.ComplexElevator.Visualizers;

[RegisterComponent]
public sealed partial class ElevatorButtonVisualsComponent : Component
{
    /// <summary>
    /// A map of the sprite states used by this visualizer indexed by the button state they correspond to.
    /// </summary>
    [DataField("spriteStateMap")]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ElevatorButtonState, string> SpriteStateMap = new()
    {
        [ElevatorButtonState.ElevatorHere] = "elevator_here",
        [ElevatorButtonState.ElevatorMoving] = "elevator_moving",
        [ElevatorButtonState.ElevatorElsewhere] = "elevator_elsewhere",
    };
}