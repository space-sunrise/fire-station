using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Shaders.Grain;
using Content.Shared._Scp.Shaders.Vignette;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    private EntityQuery<ActiveCloseFearComponent> _activeCloseFearQuery;

    private void InitializeCloseFear()
    {
        SubscribeLocalEvent<ActiveProximityTargetComponent, ComponentShutdown>(OnActiveProximityShutdown);

        _activeCloseFearQuery = GetEntityQuery<ActiveCloseFearComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateCloseFear();
    }

    private void UpdateCloseFear()
    {
        var query = EntityQueryEnumerator<ActiveProximityTargetComponent, FearComponent>();
        while (query.MoveNext(out var uid, out var proximity, out var fear))
        {
            SyncCloseFear((uid, fear), (uid, proximity));
        }
    }

    private void SyncCloseFear(Entity<FearComponent> ent, Entity<ActiveProximityTargetComponent> proximity)
    {
        if (!_mobState.IsAlive(ent))
        {
            ClearCloseFear(ent);
            return;
        }

        if (!_fearSourceQuery.TryComp(proximity.Comp.Receiver, out var source))
        {
            ClearCloseFear(ent);
            return;
        }

        if (source.UponComeCloser == FearState.None)
        {
            ClearCloseFear(ent);
            return;
        }

        if (source.PhobiaType.HasValue && !ent.Comp.Phobias.Contains(source.PhobiaType.Value))
        {
            ClearCloseFear(ent);
            return;
        }

        var blockerLevel = _proximity.GetLightOfSightBlockerLevel(proximity.Comp.Receiver, ent);
        if (blockerLevel > ent.Comp.ProximityBlockerLevel)
        {
            ClearCloseFear(ent);
            return;
        }

        if (!_watching.IsWatchedBy(proximity.Comp.Receiver, ent, checkProximity: false, useFov: false, checkBlinking: false))
        {
            ClearCloseFear(ent);
            return;
        }

        var sourceCoords = Transform(proximity.Comp.Receiver).Coordinates;
        var targetCoords = Transform(ent).Coordinates;
        if (!sourceCoords.TryDistance(EntityManager, targetCoords, out var range))
        {
            ClearCloseFear(ent);
            return;
        }

        if (range > proximity.Comp.CloseRange)
        {
            ClearCloseFear(ent);
            return;
        }

        var hadActive = _activeCloseFearQuery.TryComp(ent, out var activeCloseFear);
        activeCloseFear ??= EnsureComp<ActiveCloseFearComponent>(ent);
        var sourceChanged = hadActive && activeCloseFear.Source != proximity.Comp.Receiver;

        if (!hadActive)
        {
            AddNegativeMoodEffect(ent, MoodSourceClose);
        }
        else if (sourceChanged)
        {
            RemoveSoundEffects(ent.Owner);
        }

        activeCloseFear.Source = proximity.Comp.Receiver;

        StartEffects(ent, source.PlayHeartbeatSound, source.PlayBreathingSound);

        if (ent.Comp.State < source.UponComeCloser)
            TrySetFearLevel(ent.AsNullable(), source.UponComeCloser);

        RecalculateEffectsStrength(ent.Owner, range, proximity.Comp.CloseRange);

        SetRangeBasedShaderStrength<GrainOverlayComponent>(
            ent.Owner,
            range,
            proximity.Comp.CloseRange,
            source.GrainShaderStrength,
            blockerLevel,
            ent.Comp);

        SetRangeBasedShaderStrength<VignetteOverlayComponent>(
            ent.Owner,
            range,
            proximity.Comp.CloseRange,
            source.VignetteShaderStrength,
            blockerLevel,
            ent.Comp);
    }

    private void OnActiveProximityShutdown(Entity<ActiveProximityTargetComponent> ent, ref ComponentShutdown args)
    {
        if (!_fearQuery.TryComp(ent, out var fear))
            return;

        ClearCloseFear((ent.Owner, fear));
    }

    private void ClearCloseFear(Entity<FearComponent> ent)
    {
        RemComp<ActiveCloseFearComponent>(ent);

        SetFearBasedShaderStrength(ent);

        RemoveSoundEffects(ent.Owner);
        RemoveCloseFearMood(ent.Owner);
    }
}
