using System.Numerics;
using System.Collections.Concurrent;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

public sealed partial class AudioMuffleSystem
{
    [Dependency] private readonly Robust.Client.Physics.PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private float _maxRayLength;
    private float _solidBaseOcclusion;
    private float _solidOcclusionPerMeter;
    private float _transparentBaseOcclusion;
    private float _transparentOcclusionPerMeter;

    private readonly ConcurrentBag<HashSet<EntityUid>> _seenPool = new();
    private static readonly HashSet<ProtoId<TagPrototype>> TransparentOccluderTags =
    [
        "Window",
        "GlassAirlock",
        "Windoor",
        "Directional",
        "SecureWindoor",
        "SecurePlasmaWindoor",
        "SecureUraniumWindoor",
    ];

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<WallMountComponent> _wallMountQuery;

    /// <summary>
    /// Initializes the custom occlusion override and binds its tuning cvars.
    /// </summary>
    private void InitializeOcclusion()
    {
        _audio.GetOcclusionOverride += Override;

        Subs.CVar(_cfg, CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingSolidBaseOcclusion, value => _solidBaseOcclusion = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingSolidOcclusionPerMeter, value => _solidOcclusionPerMeter = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingTransparentBaseOcclusion, value => _transparentBaseOcclusion = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingTransparentOcclusionPerMeter, value => _transparentOcclusionPerMeter = value, true);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _wallMountQuery = GetEntityQuery<WallMountComponent>();
    }

    /// <summary>
    /// Unregisters the custom occlusion override.
    /// </summary>
    private void ShutdownOcclusion()
    {
        _audio.GetOcclusionOverride -= Override;
    }

    /// <summary>
    /// Calculates content-side occlusion by summing contributions from every blocker intersected by the ray.
    /// </summary>
    private float Override(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt = null)
    {
        if (distance <= 0.1f)
            return 0f;

        var rayLength = MathF.Min(distance, _maxRayLength);
        var ray = new CollisionRay(listener.Position, delta / distance, _audio.OcclusionCollisionMask);

        var seen = RentSeenBuffer();

        try
        {
            if (ignoredEnt != null)
                seen.Add(ignoredEnt.Value);

            AddWallMountOcclusionExemptions(listener.Position, ignoredEnt, seen);
            var occlusion = 0f;

            foreach (var hit in _physics.IntersectRayWithPredicate(
                         listener.MapId,
                         ray,
                         seen,
                         static (uid, seenState) => !seenState.Add(uid),
                         rayLength,
                         returnOnFirstHit: false))
            {
                var blockerType = ClassifyBlocker(hit.HitEntity);
                if (blockerType == LineOfSightBlockerLevel.None)
                    continue;

                var penetration = GetPenetrationDistance(hit.HitEntity, ray);
                if (penetration <= 0f)
                    continue;

                occlusion += blockerType switch
                {
                    LineOfSightBlockerLevel.Solid =>
                        _solidBaseOcclusion + penetration * _solidOcclusionPerMeter,

                    LineOfSightBlockerLevel.Transparent =>
                        _transparentBaseOcclusion + penetration * _transparentOcclusionPerMeter,

                    _ => 0f,
                };
            }

            return occlusion;
        }
        finally
        {
            ReturnSeenBuffer(seen);
        }
    }

    /// <summary>
    /// Adds same-tile anchored occluders to the ignore set for eligible wall-mount sources, but only when the
    /// listener is on the permitted side of the mount.
    /// </summary>
    private void AddWallMountOcclusionExemptions(Vector2 listenerPosition, EntityUid? source, HashSet<EntityUid> seen)
    {
        if (source == null || !_wallMountQuery.TryComp(source.Value, out var wallMount))
            return;

        var xform = Transform(source.Value);

        if (!wallMount.IgnoreAudioOcclusion)
            return;

        if (!xform.GridUid.HasValue|| !_gridQuery.TryComp(xform.GridUid, out var grid))
            return;

        if (!ShouldIgnoreWallMountOcclusion(listenerPosition, xform, wallMount))
            return;

        var tileIndices = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
        seen.UnionWith(_map.GetAnchoredEntities(xform.GridUid.Value, grid, tileIndices));
    }

    /// <summary>
    /// Uses the wall-mount interaction arc to determine whether same-tile blockers should be ignored for audio.
    /// </summary>
    private bool ShouldIgnoreWallMountOcclusion(
        Vector2 listenerPosition,
        TransformComponent xform,
        WallMountComponent wallMount)
    {
        if (wallMount.Arc >= Math.Tau)
            return true;

        var (worldPosition, worldRotation) = _transform.GetWorldPositionRotation(xform);
        var angle = Angle.FromWorldVec(listenerPosition - worldPosition);
        var angleDelta = (wallMount.Direction + worldRotation - angle).Reduced().FlipPositive();

        return angleDelta < wallMount.Arc / 2 || Math.Tau - angleDelta < wallMount.Arc / 2;
    }

    /// <summary>
    /// Rents a per-call scratch set used to de-duplicate ray hits by entity.
    /// </summary>
    private HashSet<EntityUid> RentSeenBuffer()
    {
        if (_seenPool.TryTake(out var seen))
            return seen;

        return new HashSet<EntityUid>(64);
    }

    /// <summary>
    /// Clears and returns a scratch set to the local pool.
    /// </summary>
    private void ReturnSeenBuffer(HashSet<EntityUid> seen)
    {
        seen.Clear();
        _seenPool.Add(seen);
    }

    /// <summary>
    /// Classifies a ray hit as a solid blocker, transparent blocker, or non-blocker.
    /// </summary>
    private LineOfSightBlockerLevel ClassifyBlocker(EntityUid uid)
    {
        if (!_physicsQuery.TryComp(uid, out var physics) || !physics.CanCollide || !physics.Hard)
            return LineOfSightBlockerLevel.None;

        var layer = (CollisionGroup) physics.CollisionLayer;

        if (layer.HasFlag(CollisionGroup.Opaque))
            return LineOfSightBlockerLevel.Solid;

        if (_tag.HasAnyTag(uid, TransparentOccluderTags))
            return LineOfSightBlockerLevel.Transparent;

        if (layer.HasFlag(CollisionGroup.Impassable) || layer.HasFlag(CollisionGroup.InteractImpassable))
            return LineOfSightBlockerLevel.Solid;

        return LineOfSightBlockerLevel.None;
    }

    /// <summary>
    /// Approximates the distance traveled by the ray inside the entity's hard AABB.
    /// </summary>
    private float GetPenetrationDistance(EntityUid uid, CollisionRay ray)
    {
        var aabb = _physics.GetHardAABB(uid);
        if (aabb.Size.LengthSquared() <= 0f)
            return 0f;

        var worldRay = (Ray) ray;

        if (!worldRay.Intersects(aabb, out _, out var entryPoint))
            return 0f;

        var reverseOrigin = entryPoint + ray.Direction * aabb.Size.Length() * 2f;

        if (!new Ray(reverseOrigin, -ray.Direction).Intersects(aabb, out _, out var exitPoint))
            return 0f;

        return (entryPoint - exitPoint).Length();
    }

    /// <summary>
    /// Updates the maximum occlusion ray length from the engine audio cvar.
    /// </summary>
    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }
}
