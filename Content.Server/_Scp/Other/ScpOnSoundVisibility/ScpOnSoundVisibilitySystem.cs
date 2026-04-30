using Content.Server.Popups;
using Content.Shared._Scp.Other.ScpOnSoundVisibility;
using Content.Shared.Flash;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Other.ScpOnSoundVisibility;

public sealed partial class ScpOnSoundVisibilitySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan VisibilityRefreshInterval = TimeSpan.FromSeconds(0.2f);

    private TimeSpan _nextVisibilityRefresh = TimeSpan.Zero;
    private readonly HashSet<EntityUid> _visibilityActiveTargets = [];
    private readonly HashSet<Entity<ScpOnSoundVisibilityComponent>> _visibilityCandidates = [];
    private readonly List<EntityUid> _visibilityRemovalQueue = [];

    private EntityQuery<ActiveScpOnSoundVisibilityComponent> _activeQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpOnSoundVisibilityViewerComponent, AfterFlashedEvent>(OnFlash);

        _activeQuery = GetEntityQuery<ActiveScpOnSoundVisibilityComponent>();
    }

    public void OnFlash(Entity<ScpOnSoundVisibilityViewerComponent> ent, ref AfterFlashedEvent args)
    {
        if (!ent.Comp.PoorEyesOnFlash)
            return;

        ent.Comp.PoorEyesight = true;
        ent.Comp.PoorEyesightTimeStart = _timing.CurTime;

        if (ent.Comp.OnFlashMessage != null)
        {
            var message = Loc.GetString(ent.Comp.OnFlashMessage, ("time", ent.Comp.PoorEyesightTime));
            _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);
        }

        EnsureComp<ActiveScpPoorEyesightComponent>(ent);
        DirtyFields(ent, ent.Comp, null, [
            nameof(ScpOnSoundVisibilityViewerComponent.PoorEyesight),
            nameof(ScpOnSoundVisibilityViewerComponent.PoorEyesightTimeStart)
        ]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateVisibilityTargets();

        var querySimple = EntityQueryEnumerator<ActiveScpPoorEyesightComponent, ScpOnSoundVisibilityViewerComponent>();
        while (querySimple.MoveNext(out var uid, out _, out var viewerComp))
        {
            if (!viewerComp.PoorEyesight)
                continue;

            if (viewerComp.PoorEyesightTimeStart == null)
                continue;

            var timeDifference = _timing.CurTime - viewerComp.PoorEyesightTimeStart.Value;

            if (timeDifference > TimeSpan.FromSeconds(viewerComp.PoorEyesightTime))
            {
                viewerComp.PoorEyesight = false;
                viewerComp.PoorEyesightTimeStart = null;

                RemCompDeferred<ActiveScpPoorEyesightComponent>(uid);
                DirtyFields(uid, viewerComp, null, [
                    nameof(ScpOnSoundVisibilityViewerComponent.PoorEyesight),
                    nameof(ScpOnSoundVisibilityViewerComponent.PoorEyesightTimeStart)
                ]);
            }
        }
    }

    public void UpdateVisibilityTargets()
    {
        if (_timing.CurTime < _nextVisibilityRefresh)
            return;

        _nextVisibilityRefresh = _timing.CurTime + VisibilityRefreshInterval;
        _visibilityActiveTargets.Clear();

        var scpQuery = EntityQueryEnumerator<ScpOnSoundVisibilityViewerComponent, TransformComponent>();
        while (scpQuery.MoveNext(out var uid, out var viewer, out var xform))
        {
            if (xform.MapID == MapId.Nullspace)
                continue;

            _visibilityCandidates.Clear();
            _entityLookup.GetEntitiesInRange(xform.Coordinates,
                viewer.VisibilityActivationRange,
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

        var activeQuery = EntityQueryEnumerator<ActiveScpOnSoundVisibilityComponent>();
        while (activeQuery.MoveNext(out var uid, out _))
        {
            if (_visibilityActiveTargets.Contains(uid))
                continue;

            _visibilityRemovalQueue.Add(uid);
        }

        foreach (var uid in _visibilityRemovalQueue)
        {
            RemCompDeferred<ActiveScpOnSoundVisibilityComponent>(uid);
        }

        _visibilityCandidates.Clear();
        _visibilityRemovalQueue.Clear();
    }

    public void EnsureActiveVisibility(Entity<ScpOnSoundVisibilityComponent> ent)
    {
        if (!_activeQuery.TryComp(ent, out var active))
        {
            active = AddComp<ActiveScpOnSoundVisibilityComponent>(ent);
            active.HideTime = ent.Comp.HideTime;
            active.MinValue = ent.Comp.MinValue;
            active.MaxValue = ent.Comp.MaxValue;
            return;
        }

        if (!MathHelper.CloseTo(active.HideTime, ent.Comp.HideTime))
        {
            active.HideTime = ent.Comp.HideTime;
            DirtyField(ent, active, nameof(ActiveScpOnSoundVisibilityComponent.HideTime));
        }

        if (active.MinValue != ent.Comp.MinValue)
        {
            active.MinValue = ent.Comp.MinValue;
            DirtyField(ent, active, nameof(ActiveScpOnSoundVisibilityComponent.MinValue));
        }

        if (active.MaxValue != ent.Comp.MaxValue)
        {
            active.MaxValue = ent.Comp.MaxValue;
            DirtyField(ent, active, nameof(ActiveScpOnSoundVisibilityComponent.MaxValue));
        }
    }
}
