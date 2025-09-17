using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096Overlay : Overlay
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly TransformSystem _transform;
    private readonly HashSet<EntityUid> _targets;

    public Scp096Overlay(TransformSystem transform, HashSet<EntityUid> targets)
    {
        IoCManager.InjectDependencies(this);

        _transform = transform;
        _targets = targets;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity == null)
            return;

        var playerPos = _transform.GetWorldPosition(_player.LocalEntity.Value);
        var nearestTargetPos = FindClosestEntity(playerPos, _targets);

        if (nearestTargetPos == null)
            return;

        args.WorldHandle.DrawLine(playerPos, nearestTargetPos.Value, Color.Red);
        args.WorldHandle.DrawCircle(nearestTargetPos.Value, 0.4f, Color.Red, false);
    }

    private Vector2? FindClosestEntity(Vector2 playerPos, HashSet<EntityUid> entities)
    {
        if (entities.Count == 0)
            return null;

        Vector2? closestEntityPos = null;
        var closestDistance = float.MaxValue;

        foreach (var entity in entities)
        {
            var entityPosition = _transform.GetWorldPosition(entity);

            var distance = Vector2.Distance(playerPos, entityPosition);

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestEntityPos = entityPosition;
        }

        return closestEntityPos;
    }
}

