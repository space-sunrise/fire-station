using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.ModifyThirst;

public sealed class ArtifactModifyThirstSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactModifyThirstComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactModifyThirstComponent> ent, ref ArtifactActivatedEvent args)
    {
        var humans = _lookup.GetEntitiesInRange<ThirstComponent>(Transform(ent).Coordinates, ent.Comp.Range);

        foreach (var uid in humans)
        {
            var modifier = _random.NextFloat(-1f, 1f);
            _thirst.ModifyThirst(uid, uid, modifier * ent.Comp.Amount);
        }
    }
}
