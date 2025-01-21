using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096Overlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly SharedTransformSystem _transform;
    private readonly HashSet<EntityUid> _targets;

    // TODO: Исправить проеб навигатора, когда EntityUid из списка сверху перестает существовать в пвс рейнже клиента
    // Это приводит к тому, что линия тупо улетает в ебеня, пока ентити снова не появится в пвс рейнже

    public Scp096Overlay(SharedTransformSystem transform,
        HashSet<EntityUid> targets)
    {
        IoCManager.InjectDependencies(this);
        _transform = transform;
        _targets = targets;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;
        if (playerEntity == null)
            return;

        var playerPos = _transform.GetWorldPosition(playerEntity.Value);
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

