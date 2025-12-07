using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Jittering;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _pendingJitteringRemoval = new ();
    private readonly Dictionary<Entity<Scp096Component>, TimeSpan> _pendingAnimations = new ();

    private EntityQuery<SleepingComponent> _sleepingQuery;
    private EntityQuery<KnockedDownComponent> _knockedDownQuery;
    private EntityQuery<StunnedComponent> _stunnedQuery;

    private void InitializeAppearance()
    {
        SubscribeLocalEvent<Scp096Component, StunnedEvent>(SetSitDown);
        SubscribeLocalEvent<Scp096Component, DownedEvent>(SetSitDown);
        SubscribeLocalEvent<Scp096Component, StoodEvent>(SetStandUp);

        _sleepingQuery = GetEntityQuery<SleepingComponent>();
        _knockedDownQuery = GetEntityQuery<KnockedDownComponent>();
        _stunnedQuery = GetEntityQuery<StunnedComponent>();
    }

    #region Update

    private void UpdateAnimations()
    {
        if (_pendingAnimations.Count == 0)
            return;

        List<Entity<Scp096Component>> toRemove = [];
        foreach (var (ent, end) in _pendingAnimations)
        {
            if (_timing.CurTime < end)
                continue;

            ent.Comp.AgroToDeadAnimation = false;
            ent.Comp.DeadToIdleAnimation = false;
            Dirty(ent);

            UpdateAppearance(ent.AsNullable());
            toRemove.Add(ent);
        }

        foreach (var ent in toRemove)
        {
            _pendingAnimations.Remove(ent);
        }
    }

    private void UpdateJittering()
    {
        if (_pendingJitteringRemoval.Count == 0)
            return;

        var toRemove = new List<EntityUid>();
        foreach (var (ent, end) in _pendingJitteringRemoval)
        {
            if (_timing.CurTime < end)
                continue;

            RemComp<JitteringComponent>(ent);
            toRemove.Add(ent);
        }

        foreach (var ent in toRemove)
        {
            _pendingJitteringRemoval.Remove(ent);
        }
    }

    #endregion

    #region Helpers and API

    private void UpdateAppearance(Entity<Scp096Component?, AppearanceComponent?> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        ActualizeAlert(ent);

        // Это существует только потому, что анимация передвижения принимает стейты напрямую
        // Иначе я бы сделал это через GenericVisualizer
        var useDownState = UseDownState(ent);
        var inRage = RageQuery.HasComp(ent);
        var isHeatingUp = HeatingUpQuery.HasComp(ent);
        var agroToDead = ent.Comp1.AgroToDeadAnimation;
        var deadToIdle = ent.Comp1.DeadToIdleAnimation;

        _appearance.SetData(ent, Scp096VisualsState.Dead, !deadToIdle && !agroToDead && useDownState, ent.Comp2);
        _appearance.SetData(ent, Scp096VisualsState.Agro, inRage && !useDownState, ent.Comp2);
        _appearance.SetData(ent, Scp096VisualsState.Heating, isHeatingUp, ent.Comp2);
        _appearance.SetData(ent, Scp096VisualsState.AgroToDead, agroToDead, ent.Comp2);
        _appearance.SetData(ent, Scp096VisualsState.DeadToIdle, deadToIdle, ent.Comp2);
        _appearance.SetData(ent, Scp096VisualsState.Idle, !agroToDead && !deadToIdle && !isHeatingUp && !inRage && !useDownState, ent.Comp2);

        Log.Debug($"useDownState = {useDownState}; inRage = {inRage}; isHeatingUp = {isHeatingUp}; agroToDead = {agroToDead}; deadToIdle = {deadToIdle}; ");
    }

    /// <summary>
    /// Проверяет, должен ли скромник находиться в лежачем состоянии.
    /// </summary>
    private bool UseDownState(EntityUid uid)
    {
        return _mobState.IsIncapacitated(uid)
               || _sleepingQuery.HasComp(uid)
               || _stunnedQuery.HasComp(uid)
               || _knockedDownQuery.HasComp(uid)
               || _standing.IsDown(uid);
    }

    private void AddToPendingAnimations(Entity<Scp096Component> ent, TimeSpan end)
    {
        if (_pendingAnimations.TryGetValue(ent, out var existingEnd))
            _pendingAnimations[ent] = TimeSpan.FromSeconds(Math.Max(end.TotalSeconds, existingEnd.TotalSeconds));
        else
            _pendingAnimations[ent] = end;
    }

    private void AddToPendingJittering(Entity<Scp096Component> ent, TimeSpan end)
    {
        if (_pendingJitteringRemoval.TryGetValue(ent, out var existingEnd))
            _pendingJitteringRemoval[ent] = TimeSpan.FromSeconds(Math.Max(end.TotalSeconds, existingEnd.TotalSeconds));
        else
            _pendingJitteringRemoval[ent] = end;
    }

    private void ToggleSitAnimation(Entity<Scp096Component?> ent, bool haveToStand)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.AgroToDeadAnimation = !haveToStand;
        ent.Comp.DeadToIdleAnimation = haveToStand;
        Dirty(ent);

        if (_timing.IsFirstTimePredicted)
            Log.Info($"AgroToDeadAnimation: {ent.Comp.AgroToDeadAnimation}, DeadToIdleAnimation: {ent.Comp.DeadToIdleAnimation}");

        UpdateAppearance(ent);
        AddToPendingAnimations((ent, ent.Comp), _timing.CurTime + ent.Comp.AnimationDuration);
    }

    private void SetSitDown<T>(Entity<Scp096Component> ent, ref T args)
    {
        ToggleSitAnimation(ent.AsNullable(), false);
    }

    private void SetStandUp<T>(Entity<Scp096Component> ent, ref T args)
    {
        ToggleSitAnimation(ent.AsNullable(), true);
    }

    #endregion
}
