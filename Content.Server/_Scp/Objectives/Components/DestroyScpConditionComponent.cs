
namespace Content.Server._Scp.Objectives.Components;

[RegisterComponent]
public sealed partial class DestroyScpConditionComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;
}
