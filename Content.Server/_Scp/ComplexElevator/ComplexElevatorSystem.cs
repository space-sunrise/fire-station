using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Content.Server.Doors.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server._Scp.ComplexElevator;

public sealed class ComplexElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorButtonComponent, InteractHandEvent>(OnButtonInteract);
    }

    private TimeSpan GetButtonUseDelay(Entity<ComplexElevatorComponent> elevator, ElevatorButtonComponent button)
    {
        return elevator.Comp.SendDelay + elevator.Comp.IntermediateDelay + TimeSpan.FromSeconds(1);
    }

    private void SetButtonDelay(EntityUid button, Entity<ComplexElevatorComponent> elevator)
    {
        if (TryComp<UseDelayComponent>(button, out var useDelay))
        {
            _useDelay.SetLength((button, useDelay), GetButtonUseDelay(elevator, Comp<ElevatorButtonComponent>(button)));
        }
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
            UpdateButtonLights(ent.Comp.ElevatorId);
            return;
        }

        var travelTime = ent.Comp.IntermediateDelay;

        Timer.Spawn(ent.Comp.DoorCloseDelay, () =>
        {
            if (!Exists(ent) || !ent.Comp.IsMoving)
                return;

            if (!CanCloseDoorsForFloor(ent.Comp.ElevatorId, ent.Comp.CurrentFloor))
            {
                ent.Comp.IsMoving = false;
                UpdateButtonLights(ent.Comp.ElevatorId);
                return;
            }

            _audio.PlayPvs(ent.Comp.StartSound, ent);
            _audio.PlayPvs(ent.Comp.TravelSound, ent);

            TryCloseDoorsForFloor(ent.Comp.ElevatorId, ent.Comp.CurrentFloor);
            KillEntitiesInTargetArea(ent, ent.Comp.IntermediateFloorId);
            ent.Comp.CurrentFloor = ent.Comp.IntermediateFloorId;
            TeleportToFloor(ent, ent.Comp.IntermediateFloorId);

            Timer.Spawn(travelTime, () =>
            {
                if (!Exists(ent))
                    return;

                KillEntitiesInTargetArea(ent, targetFloor);
                ent.Comp.CurrentFloor = targetFloor;
                TeleportToFloor(ent, targetFloor);
                OpenDoorsForFloor(ent.Comp.ElevatorId, targetFloor);

                _audio.PlayPvs(ent.Comp.ArrivalSound, ent);

                ent.Comp.IsMoving = false;
                UpdateButtonLights(ent.Comp.ElevatorId);
            });
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
            foreach (var entUid in intersectingEntities)
            {
                if (entUid == uid || HasComp<ElevatorDoorComponent>(entUid))
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
        if (TryFindElevator(ent.Comp.ElevatorId, out var elevator))
        {
            if (elevator.Value.Comp.IsMoving)
                return;

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
            SetButtonDelay(ent, elevator.Value);
        }
        args.Handled = true;
    }

    private bool TryFindElevator(string elevatorId, [NotNullWhen(true)] out Entity<ComplexElevatorComponent>? ent)
    {
        var query = EntityQueryEnumerator<ComplexElevatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ElevatorId == elevatorId)
            {
                ent = (uid, comp);
                return true;
            }
        }
        ent = null;
        return false;
    }

    public void MoveToFloor(Entity<ComplexElevatorComponent> ent, string targetFloor)
    {
        if (ent.Comp.IsMoving || !ent.Comp.Floors.Contains(targetFloor) || ent.Comp.CurrentFloor == targetFloor)
            return;

        if (!CanCloseDoorsForFloor(ent.Comp.ElevatorId, ent.Comp.CurrentFloor))
            return;

        ent.Comp.IsMoving = true;
        UpdateButtonLights(ent.Comp.ElevatorId);

        Timer.Spawn(ent.Comp.SendDelay, () =>
        {
            if (!Exists(ent) || !ent.Comp.IsMoving)
                return;

            StartMovement(ent, targetFloor);
        });
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

    private bool CanCloseDoorsForFloor(string elevatorId, string floor)
    {
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId == elevatorId && doorComp.Floor == floor)
            {
                if (IsDoorBlocked(doorUid))
                    return false;
            }
        }
        return true;
    }

    private bool TryCloseDoorsForFloor(string elevatorId, string floor)
    {
        var allClosed = true;
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId == elevatorId && doorComp.Floor == floor)
            {
                if (!_doorSystem.TryClose(doorUid))
                    allClosed = false;
            }
        }
        return allClosed;
    }

    private bool IsDoorBlocked(EntityUid doorUid)
    {
        var aabb = _lookup.GetWorldAABB(doorUid, Transform(doorUid)).Enlarged(-0.1f);
        var intersectingEntities = _lookup.GetEntitiesIntersecting(Transform(doorUid).MapID, aabb, LookupFlags.Dynamic | LookupFlags.Sensors);
        foreach (var ent in intersectingEntities)
        {
            if (ent != doorUid && !HasComp<ElevatorDoorComponent>(ent))
            {
                return true;
            }
        }
        return false;
    }

    private void KillEntitiesInTargetArea(Entity<ComplexElevatorComponent> elevator, string floorId)
    {
        var query = EntityQueryEnumerator<ElevatorPointComponent>();
        while (query.MoveNext(out var pointUid, out var pointComp))
        {
            if (pointComp.FloorId != floorId)
                continue;

            var pointTransform = Transform(pointUid);

            var aabb = _lookup.GetWorldAABB(elevator.Owner, pointTransform);
            var intersectingEntities = _lookup.GetEntitiesIntersecting(pointTransform.MapID, aabb, LookupFlags.Dynamic | LookupFlags.Sensors);

            foreach (var entUid in intersectingEntities)
            {
                if (entUid == elevator.Owner)
                    continue;

                var damage = new DamageSpecifier();
                damage.DamageDict["Blunt"] = 2000;
                _damageable.TryChangeDamage(entUid, damage, true);
            }
            break;
        }
    }

    private void UpdateButtonLights(string elevatorId)
    {
        if (!TryFindElevator(elevatorId, out var elevator))
            return;

        var query = EntityQueryEnumerator<ElevatorButtonComponent>();
        while (query.MoveNext(out var buttonUid, out var buttonComp))
        {
            if (buttonComp.ElevatorId != elevatorId)
                continue;

            Color color = Color.FromHex("#FF0000");
            if (buttonComp.ButtonType == ElevatorButtonType.CallButton)
            {
                if (buttonComp.Floor == elevator.Value.Comp.CurrentFloor)
                    color = Color.FromHex("#00FF00");
                else if (elevator.Value.Comp.IsMoving)
                    color = Color.FromHex("#FFFF00");
                else if (buttonComp.Floor != elevator.Value.Comp.CurrentFloor)
                    color = Color.FromHex("#FF0000");
            }

            if (TryComp<PointLightComponent>(buttonUid, out var light))
            {
                _pointLightSystem.SetColor(buttonUid, color, light);
            }
        }
    }

}