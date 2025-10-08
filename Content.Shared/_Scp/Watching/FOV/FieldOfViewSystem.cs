using System.Numerics;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.MouseRotator;
using Robust.Shared.Map;

namespace Content.Shared._Scp.Watching.FOV;

public sealed partial class FieldOfViewSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<FieldOfViewComponent> _fovQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldOfViewComponent, BuckledEvent>(OnBuckle);
        SubscribeLocalEvent<FieldOfViewComponent, UnbuckledEvent>(OnUnbuckle);

        _fovQuery = GetEntityQuery<FieldOfViewComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnBuckle(Entity<FieldOfViewComponent> ent, ref BuckledEvent args)
    {
        EnsureComp<MouseRotatorComponent>(ent);
    }

    private void OnUnbuckle(Entity<FieldOfViewComponent> ent, ref UnbuckledEvent args)
    {
        if (TryComp<CombatModeComponent>(ent, out var combat) && combat.IsInCombatMode)
            return;

        RemComp<MouseRotatorComponent>(ent);
    }

    /// <summary>
    /// Проверяет, находится ли цель в поле зрения смотрящего.
    /// </summary>
    /// <param name="viewer">Сущность, которая смотрит.</param>
    /// <param name="target">Сущность, которую проверяют.</param>
    /// <param name="fovAngleOverride">Полный угол поля зрения в градусах (например, 120).</param>
    /// <returns>True, если цель в поле зрения.</returns>
    public bool IsInViewAngle(Entity<FieldOfViewComponent?> viewer, EntityUid target, float? fovAngleOverride = null)
    {
        if (!Resolve(viewer, ref viewer.Comp))
            return false;

        var angle = FindAngleBetween(viewer.Owner, target);

        if (float.IsNaN(angle))
            return false;

        var fovAngle = fovAngleOverride ?? Math.Clamp(viewer.Comp.Angle + viewer.Comp.AngleTolerance, 0f, 360f);

        // Сравниваем с ПОЛОВИНОЙ угла, так как FindAngleBetween считает угол от центральной линии взгляда.
        // Если угол до цели меньше половины FOV, значит, она внутри конуса зрения.
        return angle < fovAngle / 2f;
    }

    public bool IsInViewAngle(EntityUid entity, Angle defaultAngle, Angle angleTolerance, EntityUid target)
    {
        var angle = FindAngleBetween(entity, target);

        if (float.IsNaN(angle))
            return false;

        var fovAngle = Math.Clamp(defaultAngle + angleTolerance, 0f, 360f);

        // Сравниваем с ПОЛОВИНОЙ угла, так как FindAngleBetween считает угол от центральной линии взгляда.
        // Если угол до цели меньше половины FOV, значит, она внутри конуса зрения.
        return angle < fovAngle / 2f;
    }

    /// <summary>
    /// Находит угол в градусах между направлением взгляда смотрящего и направлением на цель.
    /// </summary>
    public float FindAngleBetween(Entity<TransformComponent?> viewer, Entity<TransformComponent?> target)
    {
        if (!Resolve(target, ref target.Comp) || !Resolve(viewer, ref viewer.Comp))
            return float.NaN;

        // Получаем "точку зрения" (голову)
        var viewerOriginCoords = GetFovOrigin(viewer);

        // Получаем позицию цели (ее центр)
        var targetCoords = new EntityCoordinates(target.Owner, Vector2.Zero);

        var viewerWorldPos = _transform.GetMoverCoordinates(viewerOriginCoords, _xformQuery);
        var targetWorldPos = _transform.GetMoverCoordinates(targetCoords, _xformQuery);

        var toTargetVector = (targetWorldPos.Position - viewerWorldPos.Position).Normalized();

        // Направление взгляда смотрящего (вектор вперед).
        var viewerForward = viewer.Comp.GridUid.HasValue
            ? _transform.GetRelativePositionRotation(viewer.Comp, viewer.Comp.GridUid.Value, _xformQuery).Rotation.ToWorldVec()
            : viewer.Comp.LocalRotation.ToWorldVec();

        // Скалярное произведение векторов.
        var dotProduct = Vector2.Dot(viewerForward, toTargetVector);
        dotProduct = Math.Clamp(dotProduct, -1.0f, 1.0f);

        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
    }

    /// <summary>
    /// Вычисляет координаты, из которых исходит поле зрения (голова), с учетом смещения.
    /// Возвращает координаты относительно родителя сущности.
    /// </summary>
    public EntityCoordinates GetFovOrigin(Entity<TransformComponent?> viewer)
    {
        if (!Resolve(viewer, ref viewer.Comp))
            return default;

        // Если у сущности нет компонента FOV, просто возвращаем ее центр.
        if (!_fovQuery.TryComp(viewer, out var fov))
            return new EntityCoordinates(viewer.Owner, Vector2.Zero);

        // Смещение уже находится в локальных координатах, поэтому просто добавляем его.
        return new EntityCoordinates(viewer.Owner, fov.Offset);
    }

    public void SetRelay(Entity<FieldOfViewComponent?> ent, EntityUid? relay)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.RelayEntity = relay;
        DirtyField(ent, nameof(FieldOfViewComponent.RelayEntity));
    }

    /// <summary>
    /// Возвращает косинус половины угла обзора. Оптимизировано для использования в шейдерах.
    /// </summary>
    public static float GetFovCosine(float fovAngle)
    {
        var halfAngleRad = MathHelper.DegreesToRadians(fovAngle / 2f);
        return MathF.Cos(halfAngleRad);
    }
}
