using System.Numerics;
using Content.Shared._Scp.Holding.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared._Scp.Holding.Systems;

public abstract partial class SharedScpHoldingSystem
{
    /*
     * Cursor-move input validation, per-holder cursor intent, and cursor-based movement helpers.
     */

    private const float CursorMoveCancelMovementDistance = 0.005f;

    private void InitializeCursorMoveEvents()
    {
        SubscribeLocalEvent<ActiveScpHolderComponent, MoveEvent>(OnHolderMove);
    }

    public bool TryMoveHeldToCursor(EntityUid holderUid, EntityCoordinates cursorCoords)
    {
        if (!_activeHolderQuery.TryComp(holderUid, out var activeHolder))
            return false;

        if (activeHolder.Target == null)
            return false;

        if (!CanMoveHeldToCursor(holderUid, cursorCoords, out var targetCoords))
            return false;

        SetHolderCursorMoveState((holderUid, activeHolder), targetCoords, active: true);
        return true;
    }

    private bool CanMoveHeldToCursor(
        EntityUid holderUid,
        EntityCoordinates cursorCoords,
        out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;

        if (!_activeHolderQuery.TryComp(holderUid, out var holder))
            return false;

        if (holder.Target is not { } heldUid)
            return false;

        if (!_activeHoldableQuery.TryComp(heldUid, out var heldComponent))
            return false;

        if (!heldComponent.Holders.Contains(holderUid))
            return false;

        var held = (heldUid, heldComponent);

        if (!TryGetHeldHoldable(held, out var holdable))
            return false;

        if (!_container.IsInSameOrNoContainer(holderUid, heldUid))
            return false;

        var desiredSoftDragDistance = GetDesiredSoftDragDistance(holdable);
        var maintenanceRange = GetHoldMaintenanceRange(holdable, desiredSoftDragDistance);

        if (!_interaction.InRangeUnobstructed(holderUid, heldUid, maintenanceRange))
            return false;

        return TryNormalizeHeldCursorMoveTargetCoordinates(holderUid, cursorCoords, out targetCoords);
    }

    private bool TryGetHolderCursorDesiredVelocity(
        Entity<ActiveScpHolderComponent> holder,
        Entity<ActiveScpHoldableComponent> held,
        ScpHoldableComponent holdable,
        float maintenanceRange,
        PhysicsComponent heldPhysics,
        out Vector2 desiredVelocity)
    {
        desiredVelocity = Vector2.Zero;

        if (!TryGetValidatedHolderCursorMoveState(holder, held, maintenanceRange, out var targetCoordinates))
            return false;

        var holderCoords = _transform.GetMapCoordinates(holder);
        var targetCoords = _transform.ToMapCoordinates(targetCoordinates);
        var heldCoords = _transform.GetMapCoordinates(held);

        if (targetCoords.MapId != heldCoords.MapId)
        {
            ClearHolderCursorMoveState(holder);
            return false;
        }

        var correction = targetCoords.Position - heldCoords.Position;
        var correctionDistance = correction.Length();

        if (!holder.Comp.CursorMoveActive && correctionDistance > holdable.SoftDragSettleTolerance)
        {
            holder.Comp.CursorMoveActive = true;
            DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorMoveActive));
        }

        if (correctionDistance <= holdable.SoftDragSettleTolerance)
        {
            if (holder.Comp.CursorMoveActive)
            {
                holder.Comp.CursorMoveActive = false;
                DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorMoveActive));
            }

            return true;
        }

        desiredVelocity = GetHolderCursorCorrectionVelocity(
            holderCoords.Position,
            heldCoords.Position,
            targetCoords.Position,
            holdable);

        if (desiredVelocity == Vector2.Zero)
            return true;

        var correctionDirection = Vector2.Normalize(desiredVelocity);
        var relativeVelocity = heldPhysics.LinearVelocity;
        var awaySpeed = MathF.Max(0f, -Vector2.Dot(relativeVelocity, correctionDirection));
        if (awaySpeed > 0f)
            desiredVelocity += correctionDirection * awaySpeed * holdable.SoftDragAwayVelocityStrength;

        desiredVelocity = ApplyCursorMoveSpeedModifier(desiredVelocity, holdable);
        return true;
    }

    private bool TryGetValidatedHolderCursorMoveState(
        Entity<ActiveScpHolderComponent> holder,
        Entity<ActiveScpHoldableComponent> held,
        float maintenanceRange,
        out EntityCoordinates targetCoordinates)
    {
        targetCoordinates = EntityCoordinates.Invalid;

        if (holder.Comp.Target != held)
        {
            ClearHolderCursorMoveState(holder);
            return false;
        }

        if (!held.Comp.Holders.Contains(holder.Owner))
        {
            ClearHolderCursorMoveState(holder);
            return false;
        }

        if (!holder.Comp.CursorTargetCoordinates.IsValid(EntityManager))
            return false;

        if (!_container.IsInSameOrNoContainer(holder.Owner, held.Owner))
        {
            ClearHolderCursorMoveState(holder);
            return false;
        }

        if (!TryClampHeldCursorMoveTargetCoordinates(
                holder.Owner,
                holder.Comp.CursorTargetCoordinates,
                maintenanceRange,
                out targetCoordinates))
        {
            ClearHolderCursorMoveState(holder);
            return false;
        }

        return true;
    }

    private Vector2 GetHolderCursorCorrectionVelocity(
        Vector2 holderPosition,
        Vector2 heldPosition,
        Vector2 targetPosition,
        ScpHoldableComponent holdable)
    {
        var correction = targetPosition - heldPosition;
        var correctionDistance = correction.Length();
        if (correctionDistance <= holdable.SoftDragSettleTolerance)
            return Vector2.Zero;

        var currentOffset = heldPosition - holderPosition;
        var desiredOffset = targetPosition - holderPosition;
        var currentDistance = currentOffset.Length();
        var desiredDistance = desiredOffset.Length();

        if (currentDistance <= holdable.SoftDragSettleTolerance ||
            desiredDistance <= holdable.SoftDragSettleTolerance)
        {
            return GetDirectCursorCorrectionVelocity(correction, correctionDistance, holdable);
        }

        var catchUpTime = GetSoftDragCatchUpTime(holdable);
        var currentDirection = currentOffset / currentDistance;
        var desiredDirection = desiredOffset / desiredDistance;
        var cross = currentDirection.X * desiredDirection.Y - currentDirection.Y * desiredDirection.X;
        var dot = Math.Clamp(Vector2.Dot(currentDirection, desiredDirection), -1f, 1f);
        var angleDelta = MathF.Atan2(cross, dot);

        var tangentDirection = cross >= 0f
            ? new Vector2(-currentDirection.Y, currentDirection.X)
            : new Vector2(currentDirection.Y, -currentDirection.X);

        var tangentialSpeed = Math.Min(
            MathF.Abs(angleDelta) * MathF.Max(currentDistance, desiredDistance) / catchUpTime,
            holdable.SoftDragMaximumCorrectionSpeed);

        var radialSpeed = Math.Clamp(
            (desiredDistance - currentDistance) / catchUpTime,
            -holdable.SoftDragMaximumCorrectionSpeed,
            holdable.SoftDragMaximumCorrectionSpeed);

        var correctionVelocity = tangentDirection * tangentialSpeed + currentDirection * radialSpeed;
        var maximumSpeedSquared = holdable.SoftDragMaximumCorrectionSpeed * holdable.SoftDragMaximumCorrectionSpeed;
        if (correctionVelocity.LengthSquared() > maximumSpeedSquared)
            correctionVelocity = Vector2.Normalize(correctionVelocity) * holdable.SoftDragMaximumCorrectionSpeed;

        return correctionVelocity;
    }

    private Vector2 GetDirectCursorCorrectionVelocity(
        Vector2 correction,
        float correctionDistance,
        ScpHoldableComponent holdable)
    {
        if (correctionDistance <= 0f)
            return Vector2.Zero;

        var correctionDirection = correction / correctionDistance;
        var correctionSpeed = Math.Min(
            correctionDistance / GetSoftDragCatchUpTime(holdable),
            holdable.SoftDragMaximumCorrectionSpeed);

        return correctionDirection * correctionSpeed;
    }

    private static Vector2 ApplyCursorMoveSpeedModifier(Vector2 desiredVelocity, ScpHoldableComponent holdable)
    {
        if (desiredVelocity == Vector2.Zero)
            return desiredVelocity;

        var speedModifier = Math.Max(0f, holdable.CursorMoveSpeedModifier ?? holdable.HolderSprintModifier);
        if (MathF.Abs(speedModifier - 1f) <= 0.0001f)
            return desiredVelocity;

        return desiredVelocity * speedModifier;
    }

    private bool TryNormalizeHeldCursorMoveTargetCoordinates(
        EntityUid holderUid,
        EntityCoordinates cursorCoords,
        out EntityCoordinates normalizedCoords)
    {
        normalizedCoords = EntityCoordinates.Invalid;

        if (!cursorCoords.IsValid(EntityManager))
            return false;

        var holderCoords = _transform.GetMapCoordinates(holderUid);
        var cursorMapCoords = _transform.ToMapCoordinates(cursorCoords);

        if (holderCoords.MapId != cursorMapCoords.MapId)
            return false;

        normalizedCoords = _transform.ToCoordinates(cursorMapCoords);
        return normalizedCoords.IsValid(EntityManager);
    }

    private bool TryClampHeldCursorMoveTargetCoordinates(
        EntityUid holderUid,
        EntityCoordinates cursorCoords,
        float maintenanceRange,
        out EntityCoordinates clampedCoords)
    {
        clampedCoords = EntityCoordinates.Invalid;

        if (!TryNormalizeHeldCursorMoveTargetCoordinates(holderUid, cursorCoords, out var normalizedCoords))
            return false;

        var holderCoords = _transform.GetMapCoordinates(holderUid);
        var cursorMapCoords = _transform.ToMapCoordinates(normalizedCoords);
        var offset = cursorMapCoords.Position - holderCoords.Position;
        var distance = offset.Length();
        var clampedPosition = cursorMapCoords.Position;

        if (distance > maintenanceRange && distance > 0f)
            clampedPosition = holderCoords.Position + offset / distance * maintenanceRange;

        clampedCoords = _transform.ToCoordinates(new MapCoordinates(clampedPosition, holderCoords.MapId));
        return clampedCoords.IsValid(EntityManager);
    }

    private void SetHolderCursorMoveState(
        Entity<ActiveScpHolderComponent> holder,
        EntityCoordinates targetCoordinates,
        bool active)
    {
        var targetChanged = holder.Comp.CursorTargetCoordinates != targetCoordinates;
        var activeChanged = holder.Comp.CursorMoveActive != active;
        if (!targetChanged && !activeChanged)
            return;

        if (targetChanged)
        {
            holder.Comp.CursorTargetCoordinates = targetCoordinates;
            DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorTargetCoordinates));
        }

        if (activeChanged)
        {
            holder.Comp.CursorMoveActive = active;
            DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorMoveActive));
        }
    }

    private void ClearHolderCursorMoveState(EntityUid holderUid)
    {
        if (_activeHolderQuery.TryComp(holderUid, out var holder))
            ClearHolderCursorMoveState((holderUid, holder));
    }

    private void ClearHolderCursorMoveState(Entity<ActiveScpHolderComponent> holder)
    {
        var targetChanged = holder.Comp.CursorTargetCoordinates != EntityCoordinates.Invalid;
        var activeChanged = holder.Comp.CursorMoveActive;
        if (!targetChanged && !activeChanged)
            return;

        if (targetChanged)
        {
            holder.Comp.CursorTargetCoordinates = EntityCoordinates.Invalid;
            DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorTargetCoordinates));
        }

        if (activeChanged)
        {
            holder.Comp.CursorMoveActive = false;
            DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.CursorMoveActive));
        }
    }

    private void ClearHeldCursorMoveStates(Entity<ActiveScpHoldableComponent> held)
    {
        foreach (var holderUid in held.Comp.Holders)
        {
            ClearHolderCursorMoveState(holderUid);
        }
    }

    private void OnHolderMove(Entity<ActiveScpHolderComponent> ent, ref MoveEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Target == null || ent.Comp.CursorTargetCoordinates == EntityCoordinates.Invalid)
            return;

        if (args.NewPosition.EntityId == args.OldPosition.EntityId &&
            (args.NewPosition.Position - args.OldPosition.Position).LengthSquared() <
            CursorMoveCancelMovementDistance * CursorMoveCancelMovementDistance)
        {
            return;
        }

        ClearHolderCursorMoveState(ent);
    }
}
