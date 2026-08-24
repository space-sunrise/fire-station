using Content.Server._Scp.Objectives.Systems;

namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent, Access(typeof(ScpBreachScpConditionSystem))]
public sealed partial class ScpBreachScpConditionComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;
}
