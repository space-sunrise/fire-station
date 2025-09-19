using System.Numerics;
using Content.Shared._Scp.Scp096;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096Overlay : Overlay
{
    private readonly TransformSystem _transform;
    private readonly Entity<Scp096Component> _entity;

    public Scp096Overlay(Entity<Scp096Component> entity, TransformSystem transform)
    {
        IoCManager.InjectDependencies(this);

        _entity = entity;
        _transform = transform;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        base.BeforeDraw(in args);

        if (_entity.Comp.Targets.Count == 0)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerPos = _transform.GetWorldPosition(_entity);
        var nearestTargetPos = FindClosestEntity(playerPos);

        if (nearestTargetPos == null)
            return;

        args.WorldHandle.DrawLine(playerPos, nearestTargetPos.Value, Color.Red);
        args.WorldHandle.DrawCircle(nearestTargetPos.Value, 0.4f, Color.Red, false);
    }

    private Vector2? FindClosestEntity(Vector2 playerPos)
    {
        Vector2? closestEntityPos = null;
        var closestDistance = float.MaxValue;

        foreach (var entity in _entity.Comp.Targets)
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

