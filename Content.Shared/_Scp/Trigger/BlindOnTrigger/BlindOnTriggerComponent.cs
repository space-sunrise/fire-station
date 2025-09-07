using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Trigger.BlindOnTrigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlindOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);
}
