using System.Linq;
using System.Numerics;
using Content.Server.Examine;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp173;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Scp173;

public class Scp173System : SharedScp173System
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
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly ExamineSystem _examineSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173DamageStructureAction>(OnStructureDamage);
        SubscribeLocalEvent<Scp173Component, Scp173ClogAction>(OnClog);
        SubscribeLocalEvent<Scp173Component, Scp173FastMovementAction>(OnFastMovement);
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
        tempSol.AddReagent("Scp173Reagent", 25);
        _puddle.TrySpillAt(coords, tempSol, out _);

        FixedPoint2 total = 0;
        var puddles = _lookup.GetEntitiesInRange<PuddleComponent>(coords, 5).ToList();
        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototypeManager);
            total = allReagents.Where(reagent => reagent.Key.ID == "Scp173Reagent").Aggregate(total, (current, reagent) => current + reagent.Value);
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

        private void OnFastMovement(Entity<Scp173Component> ent, ref Scp173FastMovementAction args)
    {
        if (args.Handled)
            return;

        var (isValidTarget, targetCoords) = ValidateAndCalculateTarget(args, ent.Comp);
        if (!isValidTarget)
            return;

        var finalPosition = CalculateFinalPosition(ent, targetCoords);
        if (finalPosition == null)
            return;

        _transformSystem.SetCoordinates(args.Performer, finalPosition.Value.SnapToGrid());
        args.Handled = true;
    }

    private (bool isValid, MapCoordinates coords) ValidateAndCalculateTarget(Scp173FastMovementAction args, Scp173Component component)
    {
        var targetCoords = _transformSystem.ToMapCoordinates(args.Target);
        var performerCoords = _transformSystem.GetMapCoordinates(args.Performer);
        var performerPos = _transformSystem.GetWorldPosition(args.Performer);

        if (!_examineSystem.InRangeUnOccluded(
            targetCoords,
            performerCoords,
            ExamineSystemShared.MaxRaycastRange,
            null))
        {
            return (false, default);
        }

        var direction = targetCoords.Position - performerPos;
        var distance = direction.Length();

        if (distance > component.MaxJumpRange)
        {
            direction = Vector2.Normalize(direction) * component.MaxJumpRange;
            targetCoords = performerCoords.Offset(direction);
        }

        return (true, targetCoords);
    }

    private EntityCoordinates? CalculateFinalPosition(Entity<Scp173Component> scpEntitiy, MapCoordinates targetCoords)
    {

        var performerPos = _transformSystem.GetWorldPosition(scpEntitiy);
        var direction = targetCoords.Position - performerPos;
        var normalizedDirection = Vector2.Normalize(direction);

        var ray = new CollisionRay(
            performerPos,
            normalizedDirection,
            collisionMask: (int)CollisionGroup.AllMask
        );

        var rayCastResults = _physicsSystem.IntersectRay(
                targetCoords.MapId,
                ray,
                direction.Length(),
                scpEntitiy,
                false
            )
            .OrderBy(x => x.Distance)
            .ToList();

        var previousHitPos = performerPos;
        foreach (var result in rayCastResults)
        {
            if (CanBreakNeck(result.HitEntity))
            {
                BreakNeck(result.HitEntity, scpEntitiy);
                previousHitPos = result.HitPos;

                var mobXform = Transform(result.HitEntity);
                _transformSystem.SetWorldPosition(scpEntitiy, mobXform.WorldPosition);

                continue;
            }

            if (IsImpassableObstacle(result.HitEntity))
            {
                var potentialPosition = result.HitPos;
                if (HasEnoughSpace(potentialPosition, scpEntitiy, targetCoords.MapId))
                {
                    return _transformSystem.ToCoordinates(new MapCoordinates(potentialPosition, targetCoords.MapId));
                }

                var safePosition = FindSafePosition(previousHitPos, result.HitPos, scpEntitiy, targetCoords.MapId);
                if (safePosition != null)
                {
                    return null;
                }

                return _transformSystem.ToCoordinates(new MapCoordinates(performerPos, targetCoords.MapId));
            }

            previousHitPos = result.HitPos;
        }

        if (HasEnoughSpace(targetCoords.Position, scpEntitiy, targetCoords.MapId))
        {
            return _transformSystem.ToCoordinates(targetCoords);
        }

        var safePositionMaybe = FindSafePosition(performerPos, targetCoords.Position, scpEntitiy, targetCoords.MapId);

        if (safePositionMaybe.HasValue)
        {
            return _transformSystem.ToCoordinates(new MapCoordinates(safePositionMaybe.Value, targetCoords.MapId));
        }

        return null;
    }

    private bool HasEnoughSpace(Vector2 position, EntityUid entityUid, MapId mapId)
    {
        var fixtureComponent = Comp<FixturesComponent>(entityUid);
        var fixture = fixtureComponent.Fixtures.Values.First();

        var transform = new Transform(position, 0);
        var halfSize = fixture.Shape.ComputeAABB(transform, 0).Extents;
        var testBox = new Box2(position - halfSize, position + halfSize);

        var query = _physicsSystem.GetCollidingEntities(mapId, testBox);

        foreach (var collidingEntity in query)
        {
            if (IsImpassableObstacle(collidingEntity.Owner))
                return false;
        }

        return true;
    }

    private Vector2? FindSafePosition(Vector2 start, Vector2 end, EntityUid entityUid, MapId mapId)
    {
        const int maxAttempts = 10;
        const float stepBack = 0.25f;

        var direction = end - start;
        var normalizedDirection = Vector2.Normalize(direction);

        for (var i = 1; i <= maxAttempts; i++)
        {
            var testPosition = end - (normalizedDirection * (stepBack * i));
            if (HasEnoughSpace(testPosition, entityUid, mapId))
            {
                return testPosition;
            }
        }

        return null;
    }

    private bool CanBreakNeck(EntityUid entity)
    {
        return HasComp<MobStateComponent>(entity);
    }

    private bool IsImpassableObstacle(EntityUid entity)
    {
        if (!TryComp<PhysicsComponent>(entity, out var collidedEntityPhysics))
            return false;

        if (!collidedEntityPhysics.Hard)
        {
            return false;
        }

        var layer = (CollisionGroup)collidedEntityPhysics.CollisionLayer;

        return layer.HasFlag(CollisionGroup.WallLayer) || layer.HasFlag(CollisionGroup.TableLayer);
    }

}
