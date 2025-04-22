using Content.Shared.Humanoid;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Scp.Research.Artifacts.Effects.AddComponentsInRadius;

public sealed class AddComponentsInRadiusSystem : BaseXAESystem<AddComponentsInRadiusComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    protected override void OnActivated(Entity<AddComponentsInRadiusComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var players = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(coords, ent.Comp.Radius);

        foreach (var player in players)
        {
            EntityManager.AddComponents(player, ent.Comp.Components, false);
        }
    }
}
