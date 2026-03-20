using Content.Server.Popups;
using Content.Shared._Scp.Scp939;
using Content.Shared.Chat;
using Content.Shared.Flash;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan VisibilityRefreshInterval = TimeSpan.FromSeconds(0.25f);

    private TimeSpan _nextVisibilityRefresh = TimeSpan.Zero;
    private readonly HashSet<EntityUid> _visibilityActiveTargets = [];
    private readonly HashSet<Entity<Scp939VisibilityComponent>> _visibilityCandidates = [];
    private readonly List<EntityUid> _visibilityRemovalQueue = [];

    private EntityQuery<ActiveScp939VisibilityComponent> _activeQuery;

    private void InitializeVisibility()
    {
        SubscribeLocalEvent<MobStateComponent, ComponentStartup>(OnMobStartup);

        SubscribeLocalEvent<ActiveScp939VisibilityComponent, EntitySpokeEvent>(OnTargetSpoke);
        SubscribeLocalEvent<ActiveScp939VisibilityComponent, EmoteEvent>(OnTargetEmote);
        SubscribeLocalEvent<ActiveScp939VisibilityComponent, DownedEvent>(OnDown);
        SubscribeLocalEvent<ItemComponent, GunShotEvent>(OnShot);

        SubscribeLocalEvent<Scp939Component, AfterFlashedEvent>(OnFlash);

        _activeQuery = GetEntityQuery<ActiveScp939VisibilityComponent>();
    }

    private void OnFlash(Entity<Scp939Component> ent, ref AfterFlashedEvent args)
    {
        ent.Comp.PoorEyesight = true;
        ent.Comp.PoorEyesightTimeStart = _timing.CurTime;

        var message = Loc.GetString("scp939-flashed", ("time", ent.Comp.PoorEyesightTime));
        _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);

        Dirty(ent);
    }

    private void OnTargetEmote(Entity<ActiveScp939VisibilityComponent> ent, ref EmoteEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnDown(Entity<ActiveScp939VisibilityComponent> ent, ref DownedEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnShot(Entity<ItemComponent> ent, ref GunShotEvent args)
    {
        if (!_activeQuery.TryComp(args.User, out var visibilityComponent))
            return;

        MobDidSomething((args.User, visibilityComponent));
    }

    private void OnMobStartup(Entity<MobStateComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<Scp939Component>(ent))
            return;

        EnsureComp<Scp939VisibilityComponent>(ent);
    }

    private void OnTargetSpoke(Entity<ActiveScp939VisibilityComponent> ent, ref EntitySpokeEvent args)
    {
        MobDidSomething(ent);
    }

    private void MobDidSomething(Entity<ActiveScp939VisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = Scp939VisibilityComponent.InitialVisibilityAcc;
        Dirty(ent);
    }

    private void UpdateVisibilityTargets()
    {
        if (_timing.CurTime < _nextVisibilityRefresh)
            return;

        _nextVisibilityRefresh = _timing.CurTime + VisibilityRefreshInterval;
        _visibilityActiveTargets.Clear();

        var scpQuery = EntityQueryEnumerator<Scp939Component, TransformComponent>();
        while (scpQuery.MoveNext(out var uid, out var scp939, out var xform))
        {
            if (xform.MapID == MapId.Nullspace)
                continue;

            _visibilityCandidates.Clear();
            _entityLookup.GetEntitiesInRange(xform.Coordinates,
                scp939.VisibilityActivationRange,
                _visibilityCandidates,
                LookupFlags.Dynamic | LookupFlags.Approximate);

            foreach (var target in _visibilityCandidates)
            {
                if (target.Owner == uid)
                    continue;

                _visibilityActiveTargets.Add(target);
                EnsureActiveVisibility(target);
            }
        }

        _visibilityRemovalQueue.Clear();

        var activeQuery = EntityQueryEnumerator<ActiveScp939VisibilityComponent>();
        while (activeQuery.MoveNext(out var uid, out _))
        {
            if (_visibilityActiveTargets.Contains(uid))
                continue;

            _visibilityRemovalQueue.Add(uid);
        }

        foreach (var uid in _visibilityRemovalQueue)
        {
            RemComp<ActiveScp939VisibilityComponent>(uid);
        }

        _visibilityCandidates.Clear();
        _visibilityRemovalQueue.Clear();
    }

    private void EnsureActiveVisibility(Entity<Scp939VisibilityComponent> ent)
    {
        var dirty = false;

        if (!_activeQuery.TryComp(ent, out var active))
        {
            active = AddComp<ActiveScp939VisibilityComponent>(ent);
            active.VisibilityAcc = Scp939VisibilityComponent.InitialVisibilityAcc;
            dirty = true;
        }

        if (!MathHelper.CloseTo(active.HideTime, ent.Comp.HideTime))
        {
            active.HideTime = ent.Comp.HideTime;
            dirty = true;
        }

        if (active.MinValue != ent.Comp.MinValue)
        {
            active.MinValue = ent.Comp.MinValue;
            dirty = true;
        }

        if (active.MaxValue != ent.Comp.MaxValue)
        {
            active.MaxValue = ent.Comp.MaxValue;
            dirty = true;
        }

        if (dirty)
            Dirty(ent, active);
    }
}
