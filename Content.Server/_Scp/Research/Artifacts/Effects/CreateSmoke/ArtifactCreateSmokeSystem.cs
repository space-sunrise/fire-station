using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;

namespace Content.Server._Scp.Research.Artifacts.Effects.CreateSmoke;

public sealed class ArtifactCreateSmokeSystem : EntitySystem
{
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactCreateSmokeComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactCreateSmokeComponent> ent, ref ArtifactActivatedEvent args)
    {
        var xform = Transform(ent);
        var smokeEntity = Spawn(ent.Comp.Prototype, xform.Coordinates.SnapToGrid());

        var solution = new Solution(ent.Comp.Reagent, ent.Comp.Quantity);
        _smokeSystem.StartSmoke(smokeEntity, solution, ent.Comp.Duration, ent.Comp.SpreadRadius);
    }
}
