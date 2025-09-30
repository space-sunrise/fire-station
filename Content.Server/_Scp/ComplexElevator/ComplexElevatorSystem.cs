using System.Numerics;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._Scp.ComplexElevator;
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

        TryMoveElevator(ent.Owner, ent.Comp, targetFloor);
    }

    private void TryMoveElevator(EntityUid uid, ComplexElevatorComponent component, string targetFloor)
    {
        Timer.Spawn(component.SendDelay, () =>
        {
            if (!Exists(uid))
                return;

            StartMovement(uid, comp, targetFloor);
        });
    }

    private void StartMovement(EntityUid uid, ComplexElevatorComponent component, string targetFloor)
    {
        EntityUid? intermediatePoint = null;
        EntityUid? targetPoint = null;

        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId == component.IntermediateFloorId)
                intermediatePoint = pointUid;
            if (pointComp.FloorId == targetFloor)
                targetPoint = pointUid;

            if (intermediatePoint != null && targetPoint != null)
                break;
        }

        if (intermediatePoint == null || targetPoint == null)
        {
            component.IsMoving = false;
            return;
        }

        var departurePort = component.CurrentFloor == component.FirstPointId ? DepartureFirst : DepartureSecond;
        _deviceLink.SendSignal(uid, departurePort, true);

        component.CurrentFloor = component.IntermediateFloorId;
        TeleportToFloor(uid, component.IntermediateFloorId);

        Timer.Spawn(component.IntermediateDelay, () =>
        {
            if (!Exists(uid))
                return;

            if (!TryComp(uid, out ComplexElevatorComponent? comp))
                return;

            comp.CurrentFloor = targetFloor;
            TeleportToFloor(uid, targetFloor);

            var arrivalPort = targetFloor == comp.FirstPointId ? ArrivalFirst : ArrivalSecond;
            _deviceLink.SendSignal(uid, arrivalPort, true);

            comp.IsMoving = false;
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