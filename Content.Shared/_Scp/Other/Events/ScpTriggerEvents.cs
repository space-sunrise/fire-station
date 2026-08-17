using Content.Shared.Actions;

namespace Content.Shared._Scp.Other.Events;

public sealed partial class ScpTriggerOnActionEvent : InstantActionEvent
{
    [DataField]
    public string? KeyOut;
}
