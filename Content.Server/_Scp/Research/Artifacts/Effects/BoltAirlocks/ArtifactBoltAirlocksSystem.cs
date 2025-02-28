using Content.Server._Scp.Helpers;
using Content.Server.Doors.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Doors.Components;

namespace Content.Server._Scp.Research.Artifacts.Effects.BoltAirlocks;

public sealed class ArtifactBoltAirlocksSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactBoltAirlocksComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactBoltAirlocksComponent> ent, ref ArtifactActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var doors = _lookup.GetEntitiesInRange<DoorBoltComponent>(coords, ent.Comp.Range);
        var reducedDoors = _scpHelpers.GetPercentageOfHashSet(doors, ent.Comp.Chance);

        foreach (var door in reducedDoors)
        {
            _door.SetBoltsDown(door, true);
        }
    }
}
