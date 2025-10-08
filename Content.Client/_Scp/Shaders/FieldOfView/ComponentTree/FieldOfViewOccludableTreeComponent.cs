using Content.Shared._Scp.Watching.FOV;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._Scp.Shaders.FieldOfView.ComponentTree;

[RegisterComponent]
public sealed partial class FieldOfViewOccludableTreeComponent : Component, IComponentTreeComponent<FieldOfViewOccludableComponent>
{
    public DynamicTree<ComponentTreeEntry<FieldOfViewOccludableComponent>> Tree { get; set; }
}
