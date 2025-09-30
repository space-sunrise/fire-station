using System.Numerics;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server._Scp.ComplexElevator;

public sealed class ComplexElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    
    private const string ArrivalFirst = "ArrivalFirst";
    private const string ArrivalSecond = "ArrivalSecond";
    private const string DepartureFirst = "DepartureFirst";
    private const string DepartureSecond = "DepartureSecond";

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ComplexElevatorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ComplexElevatorComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<ComplexElevatorComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.CurrentFloor == ent.Comp.IntermediateFloorId)
            return;

        var currentFloorExists = false;
        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId == ent.Comp.CurrentFloor)
            {
                currentFloorExists = true;
                break;
            }
        }

        if (!currentFloorExists)
            return;

        if (ent.Comp.CurrentFloor != ent.Comp.FirstPointId && ent.Comp.CurrentFloor != ent.Comp.SecondPointId)
           return;

        var arrivalPort = ent.Comp.CurrentFloor == ent.Comp.FirstPointId ? ArrivalFirst : ArrivalSecond;
        _deviceLink.SendSignal(ent.Owner, arrivalPort, true);
    }

    private void OnSignalReceived(Entity<ComplexElevatorComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.IsMoving)
            return;

        if (args.Port != "ElevatorSend")
            return;

        ent.Comp.IsMoving = true;

        var targetFloor = string.Empty;
        if (ent.Comp.CurrentFloor == ent.Comp.FirstPointId)
        {
            targetFloor = ent.Comp.SecondPointId;
        }
        else if (ent.Comp.CurrentFloor == ent.Comp.SecondPointId)
        {
            targetFloor = ent.Comp.FirstPointId;
        }
        else
        {
            ent.Comp.IsMoving = false;
            return;
        }

        TryMoveElevator(ent, targetFloor);
    }

    private void TryMoveElevator(Entity<ComplexElevatorComponent> ent, string targetFloor)
    {
        Timer.Spawn(ent.Comp.SendDelay, () =>
        {
            if (!Exists(ent.Owner))
                return;

            StartMovement(ent, targetFloor);
        });
    }

    private void StartMovement(Entity<ComplexElevatorComponent> ent, string targetFloor)
    {
        EntityUid? intermediatePoint = null;
        EntityUid? targetPoint = null;

        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId == ent.Comp.IntermediateFloorId)
                intermediatePoint = pointUid;
            if (pointComp.FloorId == targetFloor)
                targetPoint = pointUid;

            if (intermediatePoint != null && targetPoint != null)
                break;
        }

        if (intermediatePoint == null || targetPoint == null)
        {
            ent.Comp.IsMoving = false;
            return;
        }

        var departurePort = ent.Comp.CurrentFloor == ent.Comp.FirstPointId ? DepartureFirst : DepartureSecond;
        _deviceLink.SendSignal(ent.Owner, departurePort, true);

        ent.Comp.CurrentFloor = ent.Comp.IntermediateFloorId;
        TeleportToFloor(ent.Owner, ent.Comp.IntermediateFloorId);

        Timer.Spawn(ent.Comp.IntermediateDelay, () =>
        {
            if (!Exists(ent.Owner))
                return;

            ent.Comp.CurrentFloor = targetFloor;
            TeleportToFloor(ent.Owner, targetFloor);

            var arrivalPort = targetFloor == ent.Comp.FirstPointId ? ArrivalFirst : ArrivalSecond;
            _deviceLink.SendSignal(ent.Owner, arrivalPort, true);

            ent.Comp.IsMoving = false;
        });
    }

    private void TeleportToFloor(EntityUid uid, string floorId)
    {
        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId != floorId)
                continue;

            var pointTransform = Transform(pointUid);
            var elevatorTransform = Transform(uid);

            var aabb = _lookup.GetWorldAABB(uid, elevatorTransform);
            var intersectingEntities = _lookup.GetEntitiesIntersecting(elevatorTransform.MapID, aabb, LookupFlags.Dynamic | LookupFlags.Sensors);

            var entitiesToTeleport = new List<(EntityUid, Vector2)>();
            foreach (EntityUid entUid in intersectingEntities)
            {
                if (entUid == uid)
                    continue;
                
                if (!Exists(entUid))
                    continue;
                var entTransform = Transform(entUid);
                var relativePos = entTransform.LocalPosition - elevatorTransform.LocalPosition;
                entitiesToTeleport.Add((entUid, relativePos));
            }

            _transform.SetCoordinates(uid, pointTransform.Coordinates);

            var newElevatorTransform = Transform(uid);

            foreach (var (entUid, relativePos) in entitiesToTeleport)
            {
                var newCoordinates = new EntityCoordinates(newElevatorTransform.ParentUid, newElevatorTransform.LocalPosition + relativePos);
                _transform.SetCoordinates(entUid, newCoordinates);
            }

            break;
        }
    }

}