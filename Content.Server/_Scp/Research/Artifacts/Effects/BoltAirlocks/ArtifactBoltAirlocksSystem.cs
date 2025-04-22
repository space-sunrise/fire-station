using Content.Server._Scp.Helpers;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Scp.Research.Artifacts.Effects.BoltAirlocks;

public sealed class ArtifactBoltAirlocksSystem : BaseXAESystem<ArtifactBoltAirlocksComponent>
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;

    protected override void OnActivated(Entity<ArtifactBoltAirlocksComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var doors = _lookup.GetEntitiesInRange<DoorBoltComponent>(coords, ent.Comp.Range, LookupFlags.Static);
        var reducedDoors = _scpHelpers.GetPercentageOfHashSet(doors, ent.Comp.Chance);

        foreach (var door in reducedDoors)
        {
            _door.SetBoltsDown(door, true);
        }
    }
}
