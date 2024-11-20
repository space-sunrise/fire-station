using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Coordinates.Helpers;

namespace Content.Server._Scp.Research.Artifacts.Triggers.SmokeArtifact;

public sealed class ScpSmokeArtifactSystem : EntitySystem
{
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScpSmokeArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ScpSmokeArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        var xform = Transform(ent);
        var smokeEntity = Spawn(ent.Comp.SmokeProtoId, xform.Coordinates.SnapToGrid());

        _smokeSystem.StartSmoke(smokeEntity, ent.Comp.SmokeSolution, ent.Comp.SmokeDuration, ent.Comp.SmokeSpreadRadius);
    }
}
