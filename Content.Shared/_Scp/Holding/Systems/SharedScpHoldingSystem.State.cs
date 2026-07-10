using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Holding.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;

namespace Content.Shared._Scp.Holding.Systems;

public abstract partial class SharedScpHoldingSystem
{
    /*
     * State-local dependencies, caches, held lifecycle, holder membership, and full-hold transitions.
     */

    [Dependency] private readonly SharedBodySystem _body = default!;

    private readonly List<EntityUid> _holdersToRemove = [];
    private readonly List<EntityUid> _holderCooldownsToApply = [];

    private EntityQuery<ActiveStateScpHoldableFullHoldComponent> _activeHoldableFullHoldStateQuery;
    private EntityQuery<ActiveScpHoldableComponent> _activeHoldableQuery;
    private EntityQuery<ScpHolderComponent> _holderConfigQuery;
    private EntityQuery<ActiveScpHolderComponent> _activeHolderQuery;
    private EntityQuery<ActiveStateScpHolderSlowdownComponent> _activeHolderSlowdownStateQuery;

    private void InitializeStateQueries()
    {
        _activeHoldableFullHoldStateQuery = GetEntityQuery<ActiveStateScpHoldableFullHoldComponent>();
        _activeHoldableQuery = GetEntityQuery<ActiveScpHoldableComponent>();
        _holderConfigQuery = GetEntityQuery<ScpHolderComponent>();
        _activeHolderQuery = GetEntityQuery<ActiveScpHolderComponent>();
        _activeHolderSlowdownStateQuery = GetEntityQuery<ActiveStateScpHolderSlowdownComponent>();
    }

    protected void UpdateHeld(Entity<ActiveScpHoldableComponent> held)
    {
        if (!TryGetHeldHoldable(held, out var holdable))
            return;

        var desiredSoftDragDistance = GetDesiredSoftDragDistance(holdable);
        var maintenanceRange = GetHoldMaintenanceRange(holdable, desiredSoftDragDistance);

        _holdersToRemove.Clear();

        foreach (var holderUid in held.Comp.Holders)
        {
            if (ShouldReleaseHolder(holderUid, held, maintenanceRange))
                _holdersToRemove.Add(holderUid);
        }

        foreach (var holderUid in _holdersToRemove)
        {
            ReleaseHolderContribution(holderUid, held, clearIfEmpty: false);
        }

        if (!_activeHoldableQuery.TryComp(held, out var refreshed))
            return;

        held = (held, refreshed);
        SyncHeldState(held);

        if (!_activeHoldableQuery.TryComp(held, out refreshed))
            return;

        held = (held, refreshed);
        if (!TryGetHeldHoldable(held, out holdable))
            return;

        UpdateSoftDrag(held, holdable, maintenanceRange, desiredSoftDragDistance);
    }

    private Entity<ActiveScpHoldableComponent> EnsureHeldState(EntityUid target)
    {
        var created = !_activeHoldableQuery.TryComp(target, out var held);
        held ??= EnsureComp<ActiveScpHoldableComponent>(target);

        if (created)
            held.SoftEscapeAvailableAt = _timing.CurTime;

        held.RequiredHolderCount = GetRequiredHolderCount(target);
        Dirty(target, held);
        return (target, held);
    }

    private void AddHolderContribution(EntityUid holderUid, Entity<ActiveScpHoldableComponent> held)
    {
        if (!held.Comp.Holders.Contains(holderUid))
        {
            held.Comp.Holders.Add(holderUid);
            Dirty(held);
        }

        var holderCreated = !_activeHolderQuery.TryComp(holderUid, out var holder);
        holder ??= EnsureComp<ActiveScpHolderComponent>(holderUid);
        SetHolderTarget((holderUid, holder), held);
        SyncHolderState((holderUid, holder));

        if (holderCreated)
            Dirty(holderUid, holder);
    }

    protected void ReleaseHolderContribution(EntityUid holderUid, EntityUid targetUid, bool clearIfEmpty)
    {
        if (!_activeHoldableQuery.TryComp(targetUid, out var holdable))
            return;

        if (!holdable.Holders.Remove(holderUid))
            return;

        DirtyField(targetUid, holdable, nameof(ActiveScpHoldableComponent.Holders));
        RemComp<ActiveScpHolderComponent>(holderUid);

        var ent = (targetUid, holdable);
        if (holdable.Holders.Count == 0)
        {
            if (clearIfEmpty)
                ClearHoldState(ent, applyImmunity: false);

            return;
        }

        SyncHeldState(ent);
    }

    protected void SyncHeldState(Entity<ActiveScpHoldableComponent> held)
    {
        if (!_activeHoldableQuery.TryComp(held, out var heldComp))
            return;

        held.Comp = heldComp;

        if (!TryGetHeldHoldable(held, out var holdable))
            return;

        var requiredHolderCount = GetRequiredHolderCount(held);
        if (held.Comp.RequiredHolderCount != requiredHolderCount)
        {
            held.Comp.RequiredHolderCount = requiredHolderCount;
            Dirty(held, held.Comp);
        }

        if (held.Comp.Holders.Count == 0)
        {
            ClearHoldState(held, applyImmunity: false);
            return;
        }

        if (held.Comp.Holders.Count >= held.Comp.RequiredHolderCount)
            EnterFullHold(held);
        else
            ExitFullHold(held);

        UpdateHolderSlowdowns(held, holdable);
        SyncPlaceholderHands(held);
    }

    private void EnterFullHold(Entity<ActiveScpHoldableComponent> held)
    {
        var fullHeldCreated = !_activeHoldableFullHoldStateQuery.TryComp(held, out var fullHeld);
        fullHeld ??= EnsureComp<ActiveStateScpHoldableFullHoldComponent>(held);

        if (fullHeldCreated)
        {
            fullHeld.StartedAt = _timing.CurTime;
            Dirty(held, fullHeld);
        }
    }

    private void ExitFullHold(Entity<ActiveScpHoldableComponent> held)
    {
        if (!_activeHoldableFullHoldStateQuery.HasComp(held))
            return;

        EndBreakoutAttempt(held, cancelDoAfter: true);
        RemComp<ActiveStateScpHoldableFullHoldComponent>(held);
    }

    private void ClearHoldState(Entity<ActiveScpHoldableComponent> held, bool applyImmunity)
    {
        if (_activeHoldableQuery.TryComp(held, out var refreshed))
            held = (held, refreshed);

        ClearHeldCursorMoveStates(held);
        EndBreakoutAttempt(held, cancelDoAfter: true);

        if (_activeHoldableFullHoldStateQuery.HasComp(held))
            RemComp<ActiveStateScpHoldableFullHoldComponent>(held);

        _holderCooldownsToApply.Clear();

        foreach (var holderUid in held.Comp.Holders)
        {
            if (applyImmunity)
                _holderCooldownsToApply.Add(holderUid);

            if (_activeHolderQuery.HasComp(holderUid))
                RemComp<ActiveScpHolderComponent>(holderUid);
            else if (_activeHolderSlowdownStateQuery.HasComp(holderUid))
                RemComp<ActiveStateScpHolderSlowdownComponent>(holderUid);
        }

        held.Comp.Holders.Clear();

        if (applyImmunity)
        {
            if (_holdableQuery.TryComp(held, out var holdable))
            {
                if (!TryComp<ScpHoldImmuneComponent>(held, out var immune))
                    immune = EnsureComp<ScpHoldImmuneComponent>(held);

                immune.ExpiresAt = _timing.CurTime + holdable.PostBreakoutImmunity;
                Dirty(held, immune);
            }
        }

        foreach (var holderUid in _holderCooldownsToApply)
        {
            ApplyFullBreakoutHolderCooldown(holderUid);
        }

        RemComp<ActiveScpHoldableComponent>(held);
    }

    private void UpdateHolderSlowdowns(Entity<ActiveScpHoldableComponent> held, ScpHoldableComponent holdable)
    {
        foreach (var holderUid in held.Comp.Holders)
        {
            SetHolderSlowdown(holderUid, holdable.HolderWalkModifier, holdable.HolderSprintModifier);
        }
    }

    private void SetHolderSlowdown(EntityUid holderUid, float walkModifier, float sprintModifier)
    {
        var slowdownCreated = !_activeHolderSlowdownStateQuery.TryComp(holderUid, out var slowdown);
        slowdown ??= EnsureComp<ActiveStateScpHolderSlowdownComponent>(holderUid);

        if (!slowdownCreated &&
            MathHelper.CloseTo(slowdown.WalkModifier, walkModifier) &&
            MathHelper.CloseTo(slowdown.SprintModifier, sprintModifier))
        {
            return;
        }

        slowdown.WalkModifier = walkModifier;
        slowdown.SprintModifier = sprintModifier;
        Dirty(holderUid, slowdown);
        _movement.RefreshMovementSpeedModifiers(holderUid);
    }

    private int GetRequiredHolderCount(EntityUid target)
    {
        var handCount = 0;
        foreach (var _ in _body.GetBodyChildrenOfType(target, BodyPartType.Hand))
        {
            handCount++;
        }

        return handCount;
    }

    private bool TryGetHeldHoldable(Entity<ActiveScpHoldableComponent> held, [NotNullWhen(true)] out ScpHoldableComponent? holdable)
    {
        if (_holdableQuery.TryComp(held, out holdable))
            return true;

        ClearHoldState(held, applyImmunity: false);
        holdable = null;
        return false;
    }

    private bool ShouldReleaseHolder(EntityUid holderUid, Entity<ActiveScpHoldableComponent> held, float maintenanceRange)
    {
        if (!_holderConfigQuery.HasComp(holderUid))
            return true;

        if (!_activeHolderQuery.TryComp(holderUid, out var holder))
            return true;

        if (holder.Target != held)
            return true;

        if (!_container.IsInSameOrNoContainer(holderUid, held.Owner))
            return true;

        return !_interaction.InRangeUnobstructed(holderUid, held.Owner, maintenanceRange);
    }

    private void SetHolderTarget(Entity<ActiveScpHolderComponent> holder, EntityUid? target)
    {
        if (holder.Comp.Target == target)
            return;

        holder.Comp.Target = target;
        DirtyField(holder, holder.Comp, nameof(ActiveScpHolderComponent.Target));
    }
}
