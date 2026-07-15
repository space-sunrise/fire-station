using Content.Server.Popups;
using Content.Shared._Scp.Other.ScpOnSoundVisibility;
using Content.Shared.Flash;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Other.ScpOnSoundVisibility;

public sealed partial class ScpOnSoundVisibilitySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan VisibilityRefreshInterval = TimeSpan.FromSeconds(0.2f);

    private TimeSpan _nextVisibilityRefresh = TimeSpan.Zero;
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _viewerActiveTargets = [];
    private readonly HashSet<Entity<ScpOnSoundVisibilityComponent>> _visibilityCandidates = [];
    private readonly HashSet<EntityUid> _viewerTargetsBuffer = [];
    private readonly List<EntityUid> _staleViewers = [];
    private readonly List<NetEntity> _netTargetsBuffer = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpOnSoundVisibilityViewerComponent, AfterFlashedEvent>(OnFlash);
    }

    private void OnFlash(Entity<ScpOnSoundVisibilityViewerComponent> ent, ref AfterFlashedEvent args)
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

            if (timeDifference > viewerComp.PoorEyesightTime)
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
        _staleViewers.Clear();
        _staleViewers.AddRange(_viewerActiveTargets.Keys);

        var viewerQuery = EntityQueryEnumerator<ScpOnSoundVisibilityViewerComponent, TransformComponent>();
        while (viewerQuery.MoveNext(out var uid, out var viewer, out var xform))
        {
            _staleViewers.Remove(uid);

            if (!_player.TryGetSessionByEntity(uid, out var session))
            {
                _viewerActiveTargets.Remove(uid);
                continue;
            }

            _viewerTargetsBuffer.Clear();

            if (xform.MapID != MapId.Nullspace)
            {
                _visibilityCandidates.Clear();
                _entityLookup.GetEntitiesInRange(xform.Coordinates,
                    viewer.VisibilityActivationRange,
                    _visibilityCandidates,
                    LookupFlags.Dynamic | LookupFlags.Approximate);

                foreach (var target in _visibilityCandidates)
                {
                    _viewerTargetsBuffer.Add(target);
                }

                _visibilityCandidates.Clear();
            }

            SyncViewerTargets((uid, viewer), session, _viewerTargetsBuffer);
        }

        foreach (var viewer in _staleViewers)
        {
            ClearViewerTargets(viewer);
        }

        _viewerTargetsBuffer.Clear();
        _staleViewers.Clear();
    }

    private void SyncViewerTargets(
        Entity<ScpOnSoundVisibilityViewerComponent> viewer,
        ICommonSession session,
        HashSet<EntityUid> nextTargets)
    {
        if (!_viewerActiveTargets.TryGetValue(viewer, out var currentTargets))
        {
            currentTargets = [];
            _viewerActiveTargets[viewer] = currentTargets;
        }

        if (currentTargets.SetEquals(nextTargets))
            return;

        _netTargetsBuffer.Clear();
        foreach (var target in nextTargets)
        {
            _netTargetsBuffer.Add(GetNetEntity(target));
        }

        RaiseNetworkEvent(
            new ScpOnSoundVisibilityTargetsEvent(GetNetEntity(viewer.Owner), _netTargetsBuffer.ToArray()),
            session);

        currentTargets.Clear();
        currentTargets.UnionWith(nextTargets);
        _netTargetsBuffer.Clear();
    }

    private void ClearViewerTargets(EntityUid viewer)
    {
        if (!_viewerActiveTargets.Remove(viewer, out _))
            return;

        if (!_player.TryGetSessionByEntity(viewer, out var session))
            return;

        RaiseNetworkEvent(new ScpOnSoundVisibilityTargetsEvent(GetNetEntity(viewer), []), session);
    }
}
