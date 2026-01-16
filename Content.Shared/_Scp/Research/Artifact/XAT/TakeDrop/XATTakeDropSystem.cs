using Content.Shared.Hands;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Shared._Scp.Research.Artifact.XAT.TakeDrop;

public sealed class XATTakeDropSystem : BaseXATSystem<XATTakeDropComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<GotEquippedHandEvent>(OnTake);
        XATSubscribeDirectEvent<GotUnequippedHandEvent>(OnDrop);
    }

    private void OnTake(Entity<XenoArtifactComponent> artifact, Entity<XATTakeDropComponent, XenoArtifactNodeComponent> node, ref GotEquippedHandEvent args)
    {
        if (!node.Comp1.TriggerOnTake)
            return;

        Trigger(artifact, node);
    }

    private void OnDrop(Entity<XenoArtifactComponent> artifact, Entity<XATTakeDropComponent, XenoArtifactNodeComponent> node, ref GotUnequippedHandEvent args)
    {
        if (!node.Comp1.TriggerOnDrop)
            return;

        Trigger(artifact, node);
    }
}
