using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Misc.TimedRemoveComponents;

[RegisterComponent]
public sealed partial class TimedRemoveComponentsComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField]
    public TimeSpan RemoveAfter = TimeSpan.FromSeconds(5);
}
