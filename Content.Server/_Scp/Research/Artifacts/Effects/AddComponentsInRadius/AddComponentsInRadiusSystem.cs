using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Humanoid;

namespace Content.Server._Scp.Research.Artifacts.Effects.AddComponentsInRadius;

public sealed class AddComponentsInRadiusSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsInRadiusComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<AddComponentsInRadiusComponent> ent, ref ArtifactActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var players = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(coords, ent.Comp.Radius);

        foreach (var player in players)
        {
            EntityManager.AddComponents(player, ent.Comp.Components, false);
        }
    }
}
