using System.Numerics;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._Scp.ComplexElevator;
using Content.Shared.DeviceLinking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._Scp.ComplexElevator;

public sealed class ComplexElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ComplexElevatorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ComplexElevatorComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ComplexElevatorComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ComplexElevatorComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, ComplexElevatorComponent component, ComponentStartup args)
    {
        var currentFloorExists = false;
        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId == component.CurrentFloor)
            {
                currentFloorExists = true;
                break;
            }
        }
        if (component.CurrentFloor == component.IntermediateFloorId)
            return;

        if (!currentFloorExists)
            return;

        if (component.CurrentFloor != component.FirstPointId && component.CurrentFloor != component.SecondPointId)
           return;

        var arrivalPort = component.CurrentFloor == component.FirstPointId ? "ArrivalFirst" : "ArrivalSecond";
        _deviceLinkSystem.SendSignal(uid, arrivalPort, true);
    }

    private void OnSignalReceived(Entity<ComplexElevatorComponent> ent, ref SignalReceivedEvent args)
    {
        var (uid, component) = ent;
        if (component.IsMoving)
            return;

        var port = args.Port;
        if (port != "ElevatorSend")
            return;

        component.IsMoving = true;

        string targetFloor = "";
        if (component.CurrentFloor == component.FirstPointId)
        {
            targetFloor = component.SecondPointId;
        }
        else if (component.CurrentFloor == component.SecondPointId)
        {
            targetFloor = component.FirstPointId;
        }
        else
        {
            component.IsMoving = false;
            return;
        }

        Timer.Spawn(component.SendDelay, () =>
        {
            if (!Exists(uid))
                return;

            if (!TryComp<ComplexElevatorComponent>(uid, out var comp))
                return;

            StartMovement(uid, comp, targetFloor);
        });
    }

    private void OnStartCollide(EntityUid uid, ComplexElevatorComponent component, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;
        if (!component.EntitiesOnElevator.Contains(other))
        {
            component.EntitiesOnElevator.Add(other);
        }
    }

    private void OnEndCollide(EntityUid uid, ComplexElevatorComponent component, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;
        component.EntitiesOnElevator.Remove(other);
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

        var departurePort = component.CurrentFloor == component.FirstPointId ? "DepartureFirst" : "DepartureSecond";
        _deviceLinkSystem.SendSignal(uid, departurePort, true);

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

            var arrivalPort = targetFloor == comp.FirstPointId ? "ArrivalFirst" : "ArrivalSecond";
            _deviceLinkSystem.SendSignal(uid, arrivalPort, true);

            comp.IsMoving = false;
        });
    }

    private void TeleportToFloor(EntityUid uid, string floorId)
    {
        if (!TryComp<ComplexElevatorComponent>(uid, out var component))
            return;

        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId == floorId)
            {
                var pointTransform = Transform(pointUid);
                var elevatorTransform = Transform(uid);

                var entitiesToTeleport = new List<(EntityUid, Vector2)>();
                foreach (EntityUid entUid in component.EntitiesOnElevator)
                {
                    if (!TryComp<TransformComponent>(entUid, out var entTransform))
                        continue;

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

}