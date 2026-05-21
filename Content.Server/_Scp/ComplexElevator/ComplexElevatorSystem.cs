using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Doors.Systems;
using Content.Server.Spawners.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Delivery;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Timing;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Scp.ComplexElevator;

public sealed partial class ComplexElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    private static readonly Color ElevatorButtonGreen = Color.FromHex("#00FF00");
    private static readonly Color ElevatorButtonYellow = Color.FromHex("#FFFF00");
    private static readonly Color ElevatorButtonRed = Color.FromHex("#FF0000");
    private const LookupFlags ElevatorPayloadLookupFlags = LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries | LookupFlags.StaticSundries;
    private const string SpawnerCategory = "Spawner";

    private readonly Dictionary<string, EntityUid> _elevatorIndex = new();
    private readonly Dictionary<string, EntityUid> _pointIndex = new();
    private readonly HashSet<EntityUid> _payloadCandidates = new();
    private readonly HashSet<EntityUid> _payloadRoots = new();
    private readonly List<(EntityUid Entity, Vector2 Offset, bool WasAnchored)> _payloadMoves = new();
    private readonly List<EntityUid> _movingToRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComplexElevatorComponent, ComponentStartup>(OnElevatorStartup);
        SubscribeLocalEvent<ComplexElevatorComponent, ComponentShutdown>(OnElevatorShutdown);
        SubscribeLocalEvent<ElevatorPointComponent, ComponentStartup>(OnPointStartup);
        SubscribeLocalEvent<ElevatorPointComponent, ComponentShutdown>(OnPointShutdown);

        SubscribeLocalEvent<ElevatorButtonComponent, InteractHandEvent>(OnButtonInteract);
        SubscribeLocalEvent<ElevatorButtonComponent, ActivateInWorldEvent>(OnButtonActivate);

        InitializeDebug();
    }

    private void OnElevatorStartup(EntityUid uid, ComplexElevatorComponent comp, ComponentStartup args)
    {
        if (!string.IsNullOrEmpty(comp.ElevatorId))
            _elevatorIndex[comp.ElevatorId] = uid;
    }

    private void OnElevatorShutdown(EntityUid uid, ComplexElevatorComponent comp, ComponentShutdown args)
    {
        if (!string.IsNullOrEmpty(comp.ElevatorId) && _elevatorIndex.TryGetValue(comp.ElevatorId, out var existing) && existing == uid)
            _elevatorIndex.Remove(comp.ElevatorId);
    }

    private void OnPointStartup(EntityUid uid, ElevatorPointComponent comp, ComponentStartup args)
    {
        if (!string.IsNullOrEmpty(comp.FloorId))
            _pointIndex[comp.FloorId] = uid;
    }

    private void OnPointShutdown(EntityUid uid, ElevatorPointComponent comp, ComponentShutdown args)
    {
        if (!string.IsNullOrEmpty(comp.FloorId) && _pointIndex.TryGetValue(comp.FloorId, out var existing) && existing == uid)
            _pointIndex.Remove(comp.FloorId);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ElevatorMovingComponent, ComplexElevatorComponent>();
        _movingToRemove.Clear();

        while (query.MoveNext(out var uid, out var moving, out var elevator))
        {
            if (moving.MovementStartTime == null)
            {
                moving.MovementStartTime = _timing.CurTime;
                Dirty(uid, moving);
                continue;
            }

            var elapsed = _timing.CurTime - moving.MovementStartTime.Value;

            if (moving.Phase == ElevatorMovementPhase.DoorClosing)
            {
                if (elapsed >= elevator.DoorCloseDelay)
                {
                    if (!CanCloseDoorsForFloor(elevator.ElevatorId, elevator.CurrentFloor))
                    {
                        _movingToRemove.Add(uid);
                        continue;
                    }

                    TryCloseDoorsForFloor(elevator.ElevatorId, elevator.CurrentFloor);

                    moving.Phase = ElevatorMovementPhase.WaitingForSend;
                    moving.MovementStartTime = _timing.CurTime;
                    Dirty(uid, moving);
                }
            }
            else if (moving.Phase == ElevatorMovementPhase.WaitingForSend)
            {
                if (elapsed >= elevator.SendDelay)
                {
                    if (elevator.UseIntermediateFloor)
                    {
                        ClearGasInTargetArea(uid, elevator.IntermediateFloorId);
                        KillEntitiesInTargetArea((uid, elevator), elevator.IntermediateFloorId);
                        elevator.CurrentFloor = elevator.IntermediateFloorId;
                        Dirty(uid, elevator);

                        TeleportToFloor(uid, elevator.IntermediateFloorId, elevator);

                        _audio.PlayPvs(elevator.TravelSound, uid);

                        moving.Phase = ElevatorMovementPhase.Travelling;
                        moving.MovementStartTime = _timing.CurTime;
                        Dirty(uid, moving);
                    }
                    else
                    {
                        ClearGasInTargetArea(uid, moving.TargetFloor);
                        KillEntitiesInTargetArea((uid, elevator), moving.TargetFloor);
                        elevator.CurrentFloor = moving.TargetFloor;
                        Dirty(uid, elevator);

                        TeleportToFloor(uid, moving.TargetFloor, elevator);

                        OpenDoorsForFloor(elevator.ElevatorId, moving.TargetFloor);
                        _audio.PlayPvs(elevator.ArrivalSound, uid);

                        _movingToRemove.Add(uid);
                    }
                }
            }
            else if (moving.Phase == ElevatorMovementPhase.Travelling)
            {
                if (elapsed >= elevator.IntermediateDelay)
                {
                    ClearGasInTargetArea(uid, moving.TargetFloor);
                    KillEntitiesInTargetArea((uid, elevator), moving.TargetFloor);
                    elevator.CurrentFloor = moving.TargetFloor;
                    Dirty(uid, elevator);

                    TeleportToFloor(uid, moving.TargetFloor, elevator);

                    OpenDoorsForFloor(elevator.ElevatorId, moving.TargetFloor);
                    _audio.PlayPvs(elevator.ArrivalSound, uid);

                    _movingToRemove.Add(uid);
                }
            }
        }

        foreach (var uid in _movingToRemove)
        {
            RemComp<ElevatorMovingComponent>(uid);
            if (TryComp<ComplexElevatorComponent>(uid, out var elevator))
            {
                UpdateButtonLights((uid, elevator));
            }
        }
    }

    private TimeSpan GetButtonUseDelay(Entity<ComplexElevatorComponent> elevator, ElevatorButtonComponent button)
    {
        return elevator.Comp.DoorCloseDelay + elevator.Comp.SendDelay + elevator.Comp.IntermediateDelay + TimeSpan.FromSeconds(1);
    }

    private void SetButtonDelay(EntityUid button, Entity<ComplexElevatorComponent> elevator)
    {
        if (TryComp<UseDelayComponent>(button, out var useDelay))
        {
            _useDelay.SetLength((button, useDelay), GetButtonUseDelay(elevator, Comp<ElevatorButtonComponent>(button)));
        }
    }

    private void TeleportToFloor(EntityUid uid, string floorId, ComplexElevatorComponent elevatorComp)
    {
        if (!TryFindPoint(floorId, out var point))
        {
            Log.Warning($"Could not find ElevatorPoint for floor {floorId} for elevator {elevatorComp.ElevatorId}");
            return;
        }

        var pointTransform = Transform(point.Value.Owner);
        var elevatorTransform = Transform(uid);

        BuildPayloadMoveList(uid, elevatorComp, elevatorTransform, pointTransform);
        var targetWorldPos = _transform.GetWorldPosition(pointTransform);

        foreach (var (entUid, offset, wasAnchored) in _payloadMoves)
        {
            var entTransform = Transform(entUid);

            if (wasAnchored && entTransform.Anchored)
                _transform.Unanchor(entUid, entTransform);

            var targetCoordinates = _transform.ToCoordinates(new MapCoordinates(targetWorldPos + offset, pointTransform.MapID));
            _transform.SetCoordinates(entUid, entTransform, targetCoordinates);
            _transform.AttachToGridOrMap(entUid, entTransform);

            if (wasAnchored && entTransform.GridUid != null)
                _transform.AnchorEntity(entUid, entTransform);
        }

        _transform.SetCoordinates(uid, pointTransform.Coordinates);
    }

    private void BuildPayloadMoveList(EntityUid elevator, ComplexElevatorComponent elevatorComp, TransformComponent elevatorTransform, TransformComponent pointTransform)
    {
        _payloadCandidates.Clear();
        _payloadRoots.Clear();
        _payloadMoves.Clear();

        var aabb = _lookup.GetWorldAABB(elevator, elevatorTransform);
        var intersectingEntities = _lookup.GetEntitiesIntersecting(elevatorTransform.MapID, aabb, ElevatorPayloadLookupFlags);

        foreach (var entUid in intersectingEntities)
        {
            if (!TryGetPayloadRoot(elevator, elevatorComp, entUid, out var root))
                continue;
            _payloadCandidates.Add(root);
        }

        foreach (var root in _payloadCandidates)
        {
            if (HasPayloadParent(root, _payloadCandidates))
                continue;
            _payloadRoots.Add(root);
        }

        var elevatorWorldPos = _transform.GetWorldPosition(elevatorTransform); // ← ADD THIS

        foreach (var root in _payloadRoots)
        {
            var entTransform = Transform(root);
            StopPulling(root);
            if (TryComp<StrapComponent>(root, out var strap))
            {
                foreach (var buckled in strap.BuckledEntities)
                {
                    StopPulling(buckled);
                }
            }
            var relativePos = _transform.GetWorldPosition(entTransform) - elevatorWorldPos;
            _payloadMoves.Add((root, relativePos, entTransform.Anchored));
        }
    }

    private bool TryGetPayloadRoot(EntityUid elevator, ComplexElevatorComponent elevatorComp, EntityUid entity, out EntityUid root)
    {
        root = entity;
        if (entity == elevator || Deleted(entity) || IsBlacklisted(entity))
            return false;

        if (!elevatorComp.TeleportBuckled && TryComp<BuckleComponent>(entity, out var blockedBuckle) && blockedBuckle.Buckled)
            return false;

        if (TryComp<BuckleComponent>(entity, out var buckle) && buckle.BuckledTo is { } strap)
        {
            if (Deleted(strap) || IsBlacklisted(strap))
                return false;

            root = strap;
        }

        return root != elevator && !Deleted(root) && !IsBlacklisted(root) && HasComp<TransformComponent>(root);
    }

    private bool HasPayloadParent(EntityUid entity, HashSet<EntityUid> payload)
    {
        var transform = Transform(entity);

        var parent = transform.ParentUid;
        while (parent.IsValid())
        {
            if (payload.Contains(parent))
                return true;

            var parentTransform = Transform(parent);
            if (parentTransform.ParentUid == parent)
                return false;

            parent = parentTransform.ParentUid;
        }


        return false;
    }

    private bool TransformcComp<T>(EntityUid entity, out object transform)
    {
        throw new NotImplementedException();
    }

    private void StopPulling(EntityUid entity)
    {
        if (TryComp<PullableComponent>(entity, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(entity, pullable);

        if (TryComp<PullerComponent>(entity, out var puller) &&
            puller.Pulling is { } pulled &&
            TryComp<PullableComponent>(pulled, out var pulledPullable))
        {
            _pulling.TryStopPull(pulled, pulledPullable);
        }
    }

    private void HandleButtonPress(Entity<ElevatorButtonComponent> button, Entity<ComplexElevatorComponent> elevator)
    {
        if (HasComp<ElevatorMovingComponent>(elevator))
            return;

        switch (button.Comp.ButtonType)
        {
            case ElevatorButtonType.CallButton:
                MoveToFloor(elevator, button.Comp.Floor);
                break;
            case ElevatorButtonType.SendElevatorUp:
                MoveUp(elevator);
                break;
            case ElevatorButtonType.SendElevatorDown:
                MoveDown(elevator);
                break;
        }
        SetButtonDelay(button, elevator);
    }

    private void OnButtonInteract(Entity<ElevatorButtonComponent> ent, ref InteractHandEvent args)
    {
        if (!TryFindElevator(ent.Comp.ElevatorId, out var elevator))
            return;

        HandleButtonPress(ent, elevator.Value);
        args.Handled = true;
    }

    private void OnButtonActivate(Entity<ElevatorButtonComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryFindElevator(ent.Comp.ElevatorId, out var elevator))
            return;

        HandleButtonPress(ent, elevator.Value);
        args.Handled = true;
    }

    private bool TryFindElevator(string elevatorId, [NotNullWhen(true)] out Entity<ComplexElevatorComponent>? ent)
    {
        if (_elevatorIndex.TryGetValue(elevatorId, out var uid) && TryComp<ComplexElevatorComponent>(uid, out var comp))
        {
            ent = (uid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    private bool TryFindPoint(string floorId, [NotNullWhen(true)] out Entity<ElevatorPointComponent>? ent)
    {
        if (_pointIndex.TryGetValue(floorId, out var uid) && TryComp<ElevatorPointComponent>(uid, out var comp))
        {
            ent = (uid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    public void MoveToFloor(Entity<ComplexElevatorComponent> ent, string targetFloor)
    {
        if (HasComp<ElevatorMovingComponent>(ent) || !ent.Comp.Floors.Contains(targetFloor) || ent.Comp.CurrentFloor == targetFloor)
            return;

        if (!CanCloseDoorsForFloor(ent.Comp.ElevatorId, ent.Comp.CurrentFloor))
            return;

        var moving = EnsureComp<ElevatorMovingComponent>(ent);
        moving.TargetFloor = targetFloor;
        moving.MovementStartTime = _timing.CurTime;
        moving.Phase = ElevatorMovementPhase.DoorClosing;
        Dirty(ent, moving);

        UpdateButtonLights(ent);
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

    private string? GetNextFloor(Entity<ComplexElevatorComponent> ent, bool up)
    {
        if (HasComp<ElevatorMovingComponent>(ent) || ent.Comp.Floors.Count == 0)
            return null;

        var currentIndex = ent.Comp.Floors.IndexOf(ent.Comp.CurrentFloor);
        if (currentIndex == -1)
            return null;

        if (up)
        {
            if (currentIndex <= 0)
                return null;
            return ent.Comp.Floors[currentIndex - 1];
        }
        else
        {
            if (currentIndex >= ent.Comp.Floors.Count - 1)
                return null;
            return ent.Comp.Floors[currentIndex + 1];
        }
    }

    private string? GetNextFloorUp(Entity<ComplexElevatorComponent> ent)
    {
        return GetNextFloor(ent, true);
    }

    private string? GetNextFloorDown(Entity<ComplexElevatorComponent> ent)
    {
        return GetNextFloor(ent, false);
    }

    private void OpenDoorsForFloor(string elevatorId, string floor)
    {
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId != elevatorId || doorComp.Floor != floor)
                continue;
            _doorSystem.TryOpen(doorUid);
        }
    }

    private bool CanCloseDoorsForFloor(string elevatorId, string floor)
    {
        if (!TryFindElevator(elevatorId, out var elevator))
            return true;

        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId != elevatorId || doorComp.Floor != floor)
                continue;
            if (IsDoorBlocked(doorUid, elevator.Value.Comp.DoorBlockCheckRange))
                return false;
        }
        return true;
    }

    private bool TryCloseDoorsForFloor(string elevatorId, string floor)
    {
        if (!TryFindElevator(elevatorId, out var elevator))
            return false;

        EntityUid? lastDoor = null;
        var query = EntityQueryEnumerator<ElevatorDoorComponent>();
        while (query.MoveNext(out var doorUid, out var doorComp))
        {
            if (doorComp.ElevatorId != elevatorId || doorComp.Floor != floor)
                continue;
            if (!_doorSystem.TryClose(doorUid))
                return false;
            lastDoor = doorUid;
        }

        if (lastDoor.HasValue)
        {
            _audio.PlayPvs(elevator.Value.Comp.StartSound, lastDoor.Value);
        }

        return true;
    }

    private bool IsDoorBlocked(EntityUid doorUid, float range)
    {
        if (Deleted(doorUid))
            return false;

        var intersectingEntities = _lookup.GetEntitiesInRange<PhysicsComponent>(Transform(doorUid).Coordinates, range, LookupFlags.Dynamic);
        foreach (var ent in intersectingEntities)
        {
            if (ent.Owner != doorUid && !HasComp<ElevatorDoorComponent>(ent.Owner))
                return true;
        }
        return false;
    }

    private void ClearGasInTargetArea(EntityUid elevator, string floorId)
    {
        if (!TryFindPoint(floorId, out var point))
            return;

        var pointTransform = Transform(point.Value.Owner);
        if (pointTransform.GridUid == null)
            return;

        if (!TryComp<MapGridComponent>(pointTransform.GridUid, out var grid))
            return;

        var aabb = _lookup.GetWorldAABB(elevator, pointTransform);
        var invMatrix = _transform.GetInvWorldMatrix(pointTransform.GridUid.Value);
        var localAabb = invMatrix.TransformBox(aabb);

        var tileEnumerator = _mapSystem.GetLocalTilesEnumerator(pointTransform.GridUid.Value, grid, localAabb);
        while (tileEnumerator.MoveNext(out var tileRef))
        {
            var gas = _atmos.GetTileMixture(pointTransform.GridUid, null, tileRef.GridIndices, excite: true);
            if (gas != null)
            {
                gas.Clear();
            }
        }
    }

    private void KillEntitiesInTargetArea(Entity<ComplexElevatorComponent> elevator, string floorId)
    {
        if (!elevator.Comp.CrushEntitiesOnArrival)
            return;

        if (!TryFindPoint(floorId, out var point))
            return;

        var pointTransform = Transform(point.Value.Owner);
        var aabb = _lookup.GetWorldAABB(elevator.Owner, pointTransform);
        var intersectingEntities = _lookup.GetEntitiesIntersecting(pointTransform.MapID, aabb, ElevatorPayloadLookupFlags);

        foreach (var entUid in intersectingEntities)
        {
            if (entUid == elevator.Owner || IsBlacklisted(entUid))
                continue;

            var damage = new DamageSpecifier();
            damage.DamageDict["Blunt"] = elevator.Comp.CrushDamage;
            _damageable.TryChangeDamage(entUid, damage, true);
        }
    }

    private bool IsBlacklisted(EntityUid entity)
    {
        if (HasComp<ComplexElevatorComponent>(entity) ||
            HasComp<ElevatorPointComponent>(entity) ||
            HasComp<ElevatorButtonComponent>(entity) ||
            HasComp<ElevatorDoorComponent>(entity) ||
            HasComp<SpawnPointComponent>(entity) ||
            HasComp<ContainerSpawnPointComponent>(entity) ||
            HasComp<ConditionalSpawnerComponent>(entity) ||
            HasComp<TimedSpawnerComponent>(entity) ||
            HasComp<RandomDecalSpawnerComponent>(entity) ||
            HasComp<EntityTableSpawnerComponent>(entity) ||
            HasComp<DeliverySpawnerComponent>(entity) ||
            HasComp<GhostRoleMobSpawnerComponent>(entity))
        {
            return true;
        }

        return HasPrototypeCategory(Prototype(entity), SpawnerCategory);
    }

    private static bool HasPrototypeCategory(EntityPrototype? prototype, string category)
    {
        if (prototype == null)
            return false;

        foreach (var protoCategory in prototype.Categories)
        {
            if (protoCategory.ID == category)
                return true;
        }

        return false;
    }

    private void UpdateButtonLights(Entity<ComplexElevatorComponent> elevator)
    {
        var isMoving = HasComp<ElevatorMovingComponent>(elevator);
        var query = EntityQueryEnumerator<ElevatorButtonComponent, PointLightComponent>();
        while (query.MoveNext(out var buttonUid, out var buttonComp, out var light))
        {
            if (buttonComp.ElevatorId != elevator.Comp.ElevatorId)
                continue;

            Color color = ElevatorButtonRed;
            if (buttonComp.ButtonType == ElevatorButtonType.CallButton)
            {
                if (isMoving)
                    color = ElevatorButtonYellow;
                else if (buttonComp.Floor == elevator.Comp.CurrentFloor)
                    color = ElevatorButtonGreen;
                else
                    color = ElevatorButtonRed;
            }

            _pointLight.SetColor(buttonUid, color, light);
        }
    }
}
