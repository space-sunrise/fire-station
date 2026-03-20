using System.Linq;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost;
using Content.Shared.Physics;
using Content.Shared.Prying.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Scp.Other.BunkerMarker;

public sealed class BunkerMarkerSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<Scp106PhantomComponent> _phantomQuery;
    private EntityQuery<Scp106Component> _scp106Query;
    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<ActiveScp096WithoutFaceComponent> _scp096Query;
    private EntityQuery<DoorComponent> _doorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _phantomQuery = GetEntityQuery<Scp106PhantomComponent>();
        _scp106Query = GetEntityQuery<Scp106Component>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _scp096Query = GetEntityQuery<ActiveScp096WithoutFaceComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();

        SubscribeLocalEvent<BunkerMarkerComponent, BeforePryEvent>(OnPryingBunkerDoor);
        SubscribeLocalEvent<BunkerMarkerComponent, MapInitEvent>(OnBunkerMarkerInit);
        SubscribeLocalEvent<BunkerMarkerComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        SubscribeLocalEvent<BunkerMarkerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<BunkerMarkerComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<BunkerMarkerComponent, PreventCollideEvent> (OnPreventCollide);
    }

    public void ReapplyMaskIfInSensorZone(EntityUid uid)
    {
        if (!_fixturesQuery.TryGetComponent(uid, out var fixtures))
            return;

        var coordinates = Transform(uid).Coordinates;
        var query = EntityQueryEnumerator<BunkerMarkerComponent, TransformComponent>();

        while (query.MoveNext(out _, out var marker, out var markerXform))
        {
            var markerCoords = new EntityCoordinates(markerXform.ParentUid, markerXform.LocalPosition);
            if (!_transform.InRange(coordinates, markerCoords, marker.Radius))
                continue;

            if (_phantomQuery.HasComp(uid))
                SetFixturesCollision(uid, fixtures, (int)(CollisionGroup.MobMask | CollisionGroup.GhostImpassable), (int)CollisionGroup.MobLayer);
            else if (_scp106Query.HasComp(uid))
                SetFixturesCollision(uid, fixtures, (int)CollisionGroup.MobMask, (int)CollisionGroup.MobLayer);

            return;
        }
    }

    private void OnStartCollide(Entity<BunkerMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != BunkerMarkerComponent.BunkerSensorFixtureId)
            return;

        var other = args.OtherEntity;

        if (!_fixturesQuery.TryGetComponent(other, out var fixtures))
            return;

        if (_phantomQuery.HasComp(other))
        {
            if (IsPassThroughActive(fixtures))
                return;

            SetFixturesCollision(other, fixtures, (int)(CollisionGroup.MobMask | CollisionGroup.GhostImpassable), (int)CollisionGroup.MobLayer);
        }
        else if (_scp106Query.HasComp(other))
        {
            SetFixturesCollision(other, fixtures, (int)CollisionGroup.MobMask, (int)CollisionGroup.MobLayer);
        }
    }

    private void OnEndCollide(Entity<BunkerMarkerComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != BunkerMarkerComponent.BunkerSensorFixtureId)
            return;

        var other = args.OtherEntity;

        if (!_fixturesQuery.TryGetComponent(other, out var fixtures))
            return;

        if (_phantomQuery.HasComp(other))
        {
            if (IsPassThroughActive(fixtures))
                return;

            SetFixturesCollision(other, fixtures, (int)(CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), (int)CollisionGroup.MobLayer);
        }
        else if (_scp106Query.HasComp(other))
        {
            SetFixturesCollision(other, fixtures, (int)CollisionGroup.SmallMobMask, (int)CollisionGroup.MobLayer);
        }
    }

    private void OnPryingBunkerDoor(Entity<BunkerMarkerComponent> ent, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (_scp096Query.HasComp(args.User))
            args.Cancelled = true;
    }

    private void OnBunkerMarkerInit(Entity<BunkerMarkerComponent> ent, ref MapInitEvent args)
    {
        // Hard blocker. Small radius
        // Blocks the 106 phantom body from passing through when the door is closed
        _fixture.TryCreateFixture(
            ent,
            shape: new PhysShapeCircle(BunkerMarkerComponent.BunkerBlockFixtureRadius),
            BunkerMarkerComponent.BunkerBlockFixtureId,
            hard: true,
            collisionMask:  (int)CollisionGroup.GhostImpassable,
            collisionLayer: (int)CollisionGroup.GhostImpassable);

        // Sensor. Fires StartCollide/EndCollide.
        //
        // Layer is GhostImpassable so the phantom can always "see" it regardless of
        // which mask state it is in. This prevents spurious EndCollideEvent when we change the phantom's mask on StartCollide
        //
        // Mask is MobMask|GhostImpassable so the sensor sees both the normal phantom layer and the pass-through layer
        _fixture.TryCreateFixture(
            ent,
            shape: new PhysShapeCircle(ent.Comp.Radius),
            BunkerMarkerComponent.BunkerSensorFixtureId,
            hard: false,
            collisionMask:  (int)(CollisionGroup.MobMask | CollisionGroup.GhostImpassable),
            collisionLayer: (int)CollisionGroup.GhostImpassable);

        if (_doorQuery.TryGetComponent(ent, out var door))
            SetBunkerFixtureCollision(ent.Owner, door.State != DoorState.Open);
    }

    private void OnDoorStateChanged(Entity<BunkerMarkerComponent> ent, ref DoorStateChangedEvent args)
    {
        SetBunkerFixtureCollision(ent.Owner, args.State != DoorState.Open);
    }

    private void SetBunkerFixtureCollision(EntityUid uid, bool canCollide)
    {
        if (!_fixturesQuery.TryGetComponent(uid, out var fixtures))
            return;

        if (!fixtures.Fixtures.TryGetValue(BunkerMarkerComponent.BunkerBlockFixtureId, out var fixture))
            return;

        var value = canCollide ? (int)CollisionGroup.GhostImpassable : 0;

        _physics.SetCollisionMask(uid, BunkerMarkerComponent.BunkerBlockFixtureId, fixture, value);
        _physics.SetCollisionLayer(uid, BunkerMarkerComponent.BunkerBlockFixtureId, fixture, value);
    }

    private void SetFixturesCollision(EntityUid uid, FixturesComponent fixtures, int mask, int layer)
    {
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            _physics.SetCollisionMask(uid, id, fixture, mask);
            _physics.SetCollisionLayer(uid, id, fixture, layer);
        }
    }

    // Pass-through sets every fixture's layer to GhostImpassable as its marker
    private static bool IsPassThroughActive(FixturesComponent fixtures)
    {
        return fixtures.Fixtures.Values.All(f => f.CollisionLayer == (int)CollisionGroup.GhostImpassable);
    }

    private void OnPreventCollide(Entity<BunkerMarkerComponent> ent, ref PreventCollideEvent args)
    {
        // Let observer ghosts pass through freely
        if (_ghostQuery.HasComp(args.OtherEntity) && !_phantomQuery.HasComp(args.OtherEntity))
            args.Cancelled = true;
    }
}
