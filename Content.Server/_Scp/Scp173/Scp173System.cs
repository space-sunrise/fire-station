using System.Linq;
using System.Numerics;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp173;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Scp173;

public sealed class Scp173System : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173DamageStructureAction>(OnStructureDamage);
        SubscribeLocalEvent<Scp173Component, Scp173ClogAction>(OnClog);
    }

    private void OnStructureDamage(Entity<Scp173Component> uid, ref Scp173DamageStructureAction args)
    {
        if (args.Handled)
            return;

        var defileRadius = 3f;
        var defileTilePryAmount = 10;

        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;
        var tiles = map.GetTilesIntersecting(Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(defileRadius * 2, defileRadius))).ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < defileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;

            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(uid, defileRadius, LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup)
        {
            // break windows/walls
            if (tags.HasComponent(ent))
            {
                if (_tag.HasTag(ent, "Window") || _tag.HasTag(ent, "Wall"))
                {
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 60);
                    _damageable.TryChangeDamage(ent, dspec, true);
                }
            }

            // randomly opens some lockers and such.
            if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                _entityStorage.OpenStorage(ent, entstorecomp);

            // chucks items
            if (items.HasComponent(ent) &&
                TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

            // flicker lights
            if (lights.HasComponent(ent))
                _ghost.DoGhostBooEvent(ent);
        }

        // TODO: Sound.

        args.Handled = true;
    }

    private void OnClog(Entity<Scp173Component> ent, ref Scp173ClogAction args)
    {
        if (args.Handled)
            return;

        var coords = Transform(ent).Coordinates;

        var tempSol = new Solution();
        tempSol.AddReagent("Blood", 25);
        _puddle.TrySpillAt(coords, tempSol, out _);

        FixedPoint2 total = 0;
        var puddles = _lookup.GetEntitiesInRange<PuddleComponent>(coords, 5).ToList();
        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototypeManager);
            total = allReagents.Where(reagent => reagent.Key.ID == "Blood").Aggregate(total, (current, reagent) => current + reagent.Value);
        }

        if (total >= 200)
        {
            var transform = Transform(args.Performer);

            foreach (var target in _lookup.GetEntitiesInRange(_transformSystem.GetMapCoordinates(args.Performer, transform), 5, flags: LookupFlags.Dynamic | LookupFlags.Static))
            {
                if (TryComp<DoorBoltComponent>(target, out var doorBoltComp) && doorBoltComp.BoltsDown)
                    _door.SetBoltsDown((target, doorBoltComp), false, predicted: true);

                if (TryComp<DoorComponent>(target, out var doorComp) && doorComp.State is not DoorState.Open)
                    _door.StartOpening(target);

                if (TryComp<LockComponent>(target, out var lockComp) && lockComp.Locked)
                    _lock.Unlock(target, args.Performer, lockComp);
            }
        }

        args.Handled = true;
    }
}
