using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Trigger.ScpTriggerOnAction;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpTriggerOnActionComponent : Component
{
    [DataField]
    public string? KeyOut = TriggerSystem.DefaultTriggerKey;

    [DataField]
    public bool CheckScpMask;
}
