using Content.Shared._Scp.Watching.FOV;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._ES.Viewcone.ComponentTree;

[RegisterComponent]
public sealed partial class ESViewconeOccludableTreeComponent : Component, IComponentTreeComponent<FieldOfViewOccludableComponent>
{
    public DynamicTree<ComponentTreeEntry<FieldOfViewOccludableComponent>> Tree { get; set; }
}
