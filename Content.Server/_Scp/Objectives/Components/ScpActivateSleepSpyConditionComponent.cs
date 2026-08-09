using Content.Server._Scp.Objectives.Systems;

namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent, Access(typeof(ScpActivateSleepSpyConditionSystem))]
public sealed partial class ScpActivateSleepSpyConditionComponent : Component
{
    /// <summary>The sleep spy target that an active Chaos Spy must activate.</summary>
    [ViewVariables]
    public EntityUid? Target;
}
