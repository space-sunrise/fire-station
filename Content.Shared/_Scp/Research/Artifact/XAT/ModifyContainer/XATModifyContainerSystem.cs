using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;
using Robust.Shared.Containers;

namespace Content.Shared._Scp.Research.Artifact.XAT.ModifyContainer;

public sealed class XATModifyContainerSystem : BaseXATSystem<XATModifyContainerComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<EntRemovedFromContainerMessage>(OnRemove);
        XATSubscribeDirectEvent<EntInsertedIntoContainerMessage>(OnInsert);
    }

    private void OnRemove(Entity<XenoArtifactComponent> artifact, Entity<XATModifyContainerComponent, XenoArtifactNodeComponent> node, ref EntRemovedFromContainerMessage args)
    {
        if (!node.Comp1.TriggerOnRemove)
            return;

        TryTrigger(artifact, node, args.Container.ID);
    }

    private void OnInsert(Entity<XenoArtifactComponent> artifact, Entity<XATModifyContainerComponent, XenoArtifactNodeComponent> node, ref EntInsertedIntoContainerMessage args)
    {
        if (!node.Comp1.TriggerOnInsert)
            return;

        TryTrigger(artifact, node, args.Container.ID);
    }

    private bool TryTrigger(Entity<XenoArtifactComponent> artifact,
        Entity<XATModifyContainerComponent, XenoArtifactNodeComponent> node,
        string targetContainer)
    {
        if (node.Comp1.ContainerId != null && targetContainer != node.Comp1.ContainerId)
            return false;

        Trigger(artifact, node);
        return true;
    }
}
