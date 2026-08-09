using Content.Server._Scp.Objectives.Systems;

namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent, Access(typeof(ScpActivateSleepSpyConditionSystem))]
public sealed partial class ScpActivateSleepSpyConditionComponent : Component
{
    /// <summary>Target that active spy going to activate.</summary>
    [ViewVariables]
    public EntityUid? Target;
}
