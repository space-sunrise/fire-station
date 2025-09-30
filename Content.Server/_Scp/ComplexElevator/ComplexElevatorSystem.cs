using System.Numerics;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Content.Server.Doors.Systems;

namespace Content.Server._Scp.ComplexElevator;

public sealed class ComplexElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComplexElevatorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ElevatorButtonComponent, InteractHandEvent>(OnButtonInteract);
    }

    private void OnComponentStartup(Entity<ComplexElevatorComponent> ent, ref ComponentStartup args)
    {
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

        if (!ent.Comp.Floors.Contains(ent.Comp.CurrentFloor))
            return;
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

        CloseDoorsForFloor(ent.Comp.ElevatorId, ent.Comp.CurrentFloor);
        ent.Comp.CurrentFloor = ent.Comp.IntermediateFloorId;
        TeleportToFloor(ent.Owner, ent.Comp.IntermediateFloorId);

        var travelTime = ent.Comp.IntermediateDelay;

        Timer.Spawn(travelTime, () =>
        {
            if (!Exists(ent.Owner))
                return;

            ent.Comp.CurrentFloor = targetFloor;
            TeleportToFloor(ent.Owner, targetFloor);
            OpenDoorsForFloor(ent.Comp.ElevatorId, targetFloor);

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

    private void OnButtonInteract(Entity<ElevatorButtonComponent> ent, ref InteractHandEvent args)
    {
        var elevator = FindElevator(ent.Comp.ElevatorId);
        if (elevator == null)
            return;

        args.Handled = true;
        switch (ent.Comp.ButtonType)
        {
            case ElevatorButtonType.CallButton:
                MoveToFloor(elevator.Value, ent.Comp.Floor);
                break;
            case ElevatorButtonType.SendElevatorUp:
                MoveUp(elevator.Value);
                break;
            case ElevatorButtonType.SendElevatorDown:
                MoveDown(elevator.Value);
                break;
        }
    }

    private Entity<ComplexElevatorComponent>? FindElevator(string elevatorId)
    {
        var query = EntityQueryEnumerator<ComplexElevatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ElevatorId == elevatorId)
                return (uid, comp);
        }
        return null;
    }

    public void MoveToFloor(Entity<ComplexElevatorComponent> ent, string targetFloor)
    {
        if (ent.Comp.IsMoving || !ent.Comp.Floors.Contains(targetFloor) || ent.Comp.CurrentFloor == targetFloor)
            return;

        ent.Comp.IsMoving = true;
        TryMoveElevator(ent, targetFloor);
    }

    public void MoveUp(Entity<ComplexElevatorComponent> ent)
    {
        var nextFloor = GetNextFloorUp(ent);
        if (nextFloor != null)
            MoveToFloor(ent, nextFloor);
    }

    public void MoveDown(Entity<ComplexElevatorComponent> ent)
    {
        var nextFloor = GetNextFloorDown(ent);
        if (nextFloor != null)
            MoveToFloor(ent, nextFloor);
    }

    private string? GetNextFloorUp(Entity<ComplexElevatorComponent> ent)
    {
        if (ent.Comp.IsMoving || ent.Comp.Floors.Count == 0)
            return null;

        var currentIndex = ent.Comp.Floors.IndexOf(ent.Comp.CurrentFloor);
        if (currentIndex == -1 || currentIndex <= 0)
            return null;

        return ent.Comp.Floors[currentIndex - 1];
    }

    private string? GetNextFloorDown(Entity<ComplexElevatorComponent> ent)
    {
        if (ent.Comp.IsMoving || ent.Comp.Floors.Count == 0)
            return null;

        var currentIndex = ent.Comp.Floors.IndexOf(ent.Comp.CurrentFloor);
        if (currentIndex == -1 || currentIndex >= ent.Comp.Floors.Count - 1)
            return null;

        return ent.Comp.Floors[currentIndex + 1];
    }

    private void OpenDoorsForFloor(string elevatorId, string floor)
    {
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId == elevatorId && doorComp.Floor == floor)
            {
                _doorSystem.TryOpen(doorUid);
            }
        }
    }

    private void CloseDoorsForFloor(string elevatorId, string floor)
    {
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId == elevatorId && doorComp.Floor == floor)
            {
                _doorSystem.TryClose(doorUid);
            }
        }
    }

}