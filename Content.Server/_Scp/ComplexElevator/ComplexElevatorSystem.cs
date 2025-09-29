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
        // Send initial arrival signal based on current floor
        var arrivalPort = component.CurrentFloor == component.FirstPointId ? "arrival-first" : "arrival-second";
        _deviceLinkSystem.SendSignal(uid, arrivalPort, true);
    }

    private void OnSignalReceived(EntityUid uid, ComplexElevatorComponent component, SignalReceivedEvent args)
    {
        if (component.IsMoving)
            return;

        var port = args.Port;
        if (port != "Send")
            return;

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
            return;
        }

        Timer.Spawn(component.SendDelay, () =>
        {
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
        // Check if intermediate and target points exist
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
                break; // Early exit when both found
        }

        if (intermediatePoint == null || targetPoint == null)
            return; // Missing points, don't move

        component.IsMoving = true;

        // Send departure signal based on current floor
        var departurePort = component.CurrentFloor == component.FirstPointId ? "departure-first" : "departure-second";
        _deviceLinkSystem.SendSignal(uid, departurePort, true);

        var startFloor = component.CurrentFloor;

        component.CurrentFloor = component.IntermediateFloorId;
        TeleportToFloor(uid, component.IntermediateFloorId);

        Timer.Spawn(component.IntermediateDelay, () =>
        {
            if (!TryComp(uid, out ComplexElevatorComponent? comp))
                return;

            comp.CurrentFloor = targetFloor;
            TeleportToFloor(uid, targetFloor);

            // Send arrival signal based on target floor
            var arrivalPort = targetFloor == comp.FirstPointId ? "arrival-first" : "arrival-second";
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
                foreach (var entUid in component.EntitiesOnElevator)
                {
                    if (!TryComp<TransformComponent>(entUid, out var entTransform))
                        continue;

                    var relativePos = entTransform.LocalPosition - elevatorTransform.LocalPosition;
                    entitiesToTeleport.Add((entUid, relativePos));
                }

                _transform.SetCoordinates(uid, pointTransform.Coordinates);

                foreach (var (entUid, relativePos) in entitiesToTeleport)
                {
                    var newCoordinates = new EntityCoordinates(pointTransform.ParentUid, pointTransform.LocalPosition + relativePos);
                    _transform.SetCoordinates(entUid, newCoordinates);
                }

                break;
            }
        }
    }

}