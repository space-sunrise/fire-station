using Content.Server._Scp.Objectives.Systems;

namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent, Access(typeof(ScpActivateSleepSpyConditionSystem))]
public sealed partial class ScpActivateSleepSpyConditionComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;
}
