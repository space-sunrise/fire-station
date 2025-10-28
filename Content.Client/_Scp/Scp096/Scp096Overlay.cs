using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096Overlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly Entity<Scp096Component> _entity;

    private readonly EntityQuery<Scp096TargetComponent> _targetQuery;
    private readonly HashSet<(Entity<SpriteComponent> ent, float alpha)> _cachedAlphas = new(128);

    public Scp096Overlay(Entity<Scp096Component> entity, TransformSystem transform)
    {
        IoCManager.InjectDependencies(this);

        _entity = entity;
        _transform = transform;
        _sprite = _ent.System<SpriteSystem>();

        _targetQuery = _ent.GetEntityQuery<Scp096TargetComponent>();
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Рисуем указатель к ближайшей цели
        DrawLineToTarget(in args);

        // Показываем ранее скрытые сущности
        ShowAllHiddenEntities();
        // Скрываем все сущности, которые не являются целями
        HideNonTargetEntities();
    }

    private void HideNonTargetEntities()
    {
        if (_entity.Comp.Targets.Count == 0 || !_entity.Comp.InRageMode)
            return;

        var query = _ent.EntityQueryEnumerator<BlinkableComponent, MobStateComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _ , out _, out var sprite))
        {
            if (_ent.IsClientSide(uid))
                continue;

            if (MathHelper.CloseTo(sprite.Color.A, 0f))
                continue;

            // Не скрываем целей скромника
            if (_targetQuery.TryComp(uid, out var target) && target.TargetedBy.Contains(_entity))
                continue;

            var entity = (uid, sprite);

            _cachedAlphas.Add((entity, sprite.Color.A));
            _sprite.SetColor(entity, sprite.Color.WithAlpha(0f));
        }
    }

    private void DrawLineToTarget(in OverlayDrawArgs args)
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

    public void ShowAllHiddenEntities()
    {
        if (_cachedAlphas.Count == 0)
            return;

        foreach (var (ent, alpha) in _cachedAlphas)
        {
            _sprite.SetColor(ent.AsNullable(), ent.Comp.Color.WithAlpha(alpha));
        }

        _cachedAlphas.Clear();
    }
}

