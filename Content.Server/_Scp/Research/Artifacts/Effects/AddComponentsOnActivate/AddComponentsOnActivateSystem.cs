using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server._Scp.Research.Artifacts.Effects.AddComponentsOnActivate;

public sealed class AddComponentsOnActivateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsOnActivateComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<AddComponentsOnActivateComponent> ent, ref ArtifactActivatedEvent args)
    {
        EntityManager.AddComponents(ent, ent.Comp.Components);
    }
}
