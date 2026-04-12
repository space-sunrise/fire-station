using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Trigger.TriggerOnSignalSwitch;

[Serializable, NetSerializable]
public enum SignalSwitchTriggerMode : byte
{
    Any,
    ActivatedOnly,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnSignalSwitchComponent : BaseTriggerOnXComponent
{
    [DataField, AutoNetworkedField]
    public SignalSwitchTriggerMode Mode = SignalSwitchTriggerMode.Any;
}
