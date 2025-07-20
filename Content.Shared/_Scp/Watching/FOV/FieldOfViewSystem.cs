using System.Numerics;

namespace Content.Shared._Scp.Watching.FOV;

public sealed class FieldOfViewSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Проверяет, находится ли цель в поле зрения смотрящего.
    /// </summary>
    /// <param name="viewer">Сущность, которая смотрит.</param>
    /// <param name="target">Сущность, которую проверяют.</param>
    /// <param name="fovAngleOverride">Полный угол поля зрения в градусах (например, 120).</param>
    /// <returns>True, если цель в поле зрения.</returns>
    public bool IsInViewAngle(Entity<FieldOfViewComponent?> viewer, EntityUid target, float? fovAngleOverride)
    {
        if (!Resolve(viewer, ref viewer.Comp))
            return false;

        var angle = FindAngleBetween(viewer.Owner, target);
        var fovAngle = fovAngleOverride ?? viewer.Comp.Angle;

        // Сравниваем с ПОЛОВИНОЙ угла, так как FindAngleBetween считает угол от центральной линии взгляда.
        // Если угол до цели меньше половины FOV, значит, она внутри конуса зрения.
        return angle < fovAngle / 2f;
    }

    /// <summary>
    /// Находит угол в градусах между направлением взгляда смотрящего и направлением на цель.
    /// </summary>
    public float FindAngleBetween(Entity<TransformComponent?> viewer, Entity<TransformComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return float.MaxValue;

        if (!Resolve(viewer, ref viewer.Comp))
            return float.MaxValue;

        var targetWorldPosition = _transform.GetMoverCoordinates(target.Owner);
        var viewerWorldPosition = _transform.GetMoverCoordinates(viewer.Owner);

        // Вектор от смотрящего к цели
        var toTarget = (targetWorldPosition.Position - viewerWorldPosition.Position).Normalized();
        // Направление взгляда смотрящего
        var viewerForward = viewer.Comp.LocalRotation.ToWorldVec();

        // Скалярное произведение векторов
        var dotProduct = Vector2.Dot(viewerForward, toTarget);

        // Ограничиваем значение, чтобы избежать ошибок с плавающей точкой в Acos
        dotProduct = Math.Clamp(dotProduct, -1.0f, 1.0f);

        // Вычисляем угол через арккосинус и переводим в градусы.
        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
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
