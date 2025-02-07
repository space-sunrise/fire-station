using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.Hunger;

public sealed class HungerArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<HungerArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        var humans = _lookup.GetEntitiesInRange<HungerComponent>(Transform(ent).Coordinates, ent.Comp.Range);

        foreach (var uid in humans)
        {
            var modifier = _random.NextFloat(-1f, 1f);
            _hunger.ModifyHunger(uid, modifier * ent.Comp.Amount);
        }
    }
}
