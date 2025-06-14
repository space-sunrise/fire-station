﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Scp.Watching;

// TODO: Отдельная система FOV с затемнением всего, что находится за спиной персонажа
public sealed partial class EyeWatchingSystem
{
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    // Возможно удваивается на 2, что приводит к использованию поля зрения в 240 градусов в реальности
    // Не ебу за математику, поэтому хз
    public const float DefaultFieldOfViewAngle = 120f;

    /// <summary>
    /// Проверяет, смотрит ли кто-то на указанную цель
    /// </summary>
    /// <param name="ent">Цель, которую проверяем</param>
    /// <param name="useFov">Нужно ли проверять поле зрения</param>
    /// <param name="fovOverride">Если нужно использовать другой угол обзора, отличный от стандартного</param>
    /// <returns>Смотрит ли на цель хоть кто-то</returns>
    public bool IsWatched(Entity<TransformComponent?> ent, bool useFov = true, float? fovOverride = null)
    {
        return IsWatched(ent, out _, useFov, fovOverride);
    }

    /// <summary>
    /// Проверяет, смотрит ли кто-то на указанную цель
    /// </summary>
    /// <param name="ent">Цель, которую проверяем</param>
    /// <param name="watchersCount">Количество смотрящих</param>
    /// <param name="useFov">Нужно ли проверять поле зрения</param>
    /// <param name="fovOverride">Если нужно использовать другой угол обзора, отличный от стандартного</param>
    /// <returns>Смотрит ли на цель хоть кто-то</returns>
    public bool IsWatched(EntityUid ent, [NotNullWhen(true)] out int? watchersCount, bool useFov = true, float? fovOverride = null)
    {
        var eyes = GetWatchers(ent);

        var isWatched = IsWatchedBy(ent, eyes, out int count , useFov, fovOverride);
        watchersCount = count;

        return isWatched;
    }

    /// <summary>
    /// Получает и возвращает всех потенциально смотрящих на указанную цель.
    /// </summary>
    /// <remarks>
    /// В методе нет проверок на дополнительные состояния, такие как моргание/закрыты ли глаза/поле зрения т.п.
    /// Единственная проверка - можно ли физически увидеть цель(т.е. не закрыта ли она стеной и т.п.)
    /// </remarks>
    /// <param name="ent">Цель, для которой ищем потенциальных смотрящих</param>
    /// <returns>Список всех, кто потенциально видит цель</returns>
    public IEnumerable<EntityUid> GetWatchers(Entity<TransformComponent?> ent)
    {
        return GetAllVisibleTo<BlinkableComponent>(ent);
    }

    /// <summary>
    /// Получает и возвращает всех потенциально смотрящих на указанную цель.
    /// </summary>
    /// <remarks>
    /// В методе нет проверок на дополнительные состояния, такие как моргание/закрыты ли глаза/поле зрения т.п.
    /// Единственная проверка - можно ли физически увидеть цель(т.е. не закрыта ли она стеной и т.п.)
    /// </remarks>
    /// <param name="ent">Цель, для которой ищем потенциальных смотрящих</param>
    /// <returns>Список всех, кто потенциально видит цель</returns>
    public IEnumerable<EntityUid> GetAllVisibleTo<T>(Entity<TransformComponent?> ent) where T : IComponent
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return [];

        return _lookup.GetEntitiesInRange<T>(ent.Comp.Coordinates, ExamineSystemShared.ExamineRange)
            .Where(eye => _examine.InRangeUnOccluded(eye, ent, ignoreInsideBlocker: false))
            .Select(e => e.Owner);
    }

    /// <summary>
    /// Проверяет, смотрят ли переданные сущности на указанную цель
    /// </summary>
    /// <param name="target">Цель</param>
    /// <param name="watchers">Список сущностей для проверки</param>
    /// <param name="watchersCount">Количество смотрящих</param>
    /// <param name="useFov">Нужно ли проверять, находится ли цель в поле зрения сущности</param>
    /// <param name="fovOverride">Если нужно перезаписать угол поля зрения</param>
    /// <returns>Смотрит ли хоть кто-то на цель</returns>
    public bool IsWatchedBy(EntityUid target, IEnumerable<EntityUid> watchers, out int watchersCount, bool useFov = true, float? fovOverride = null)
    {
        var isWatched = IsWatchedBy(target, watchers, out IEnumerable<EntityUid> viewers, useFov, fovOverride);
        watchersCount = viewers.Count();

        return isWatched;
    }

    /// <summary>
    /// Проверяет, смотрят ли переданные сущности на указанную цель. Передает список всех сущностей, что действительно смотрят на цель
    /// </summary>
    /// <param name="target">Цель</param>
    /// <param name="watchers">Список сущностей для проверки</param>
    /// <param name="viewers">Список всех сущностей, что действительно смотрят на цель</param>
    /// <param name="useFov">Нужно ли проверять, находится ли цель в поле зрения сущности</param>
    /// <param name="fovOverride">Если нужно перезаписать угол поля зрения</param>
    /// <returns>Смотрит ли хоть кто-то на цель</returns>
    public bool IsWatchedBy(EntityUid target, IEnumerable<EntityUid> watchers, out IEnumerable<EntityUid> viewers, bool useFov = true, float? fovOverride = null)
    {
        viewers = watchers
            .Where(eye => CanBeWatched(eye, target))
            .Where(eye => !IsEyeBlinded(eye, target, useFov, fovOverride));

        return viewers.Any();
    }

    /// <summary>
    /// Проверяет, может ли цель вообще быть увидена смотрящим
    /// </summary>
    /// <remarks>
    /// Проверка заключается в поиске базовых компонентов, без которых Watching система не будет работать
    /// </remarks>
    /// <param name="viewer">Смотрящий, который в теории может увидеть цель</param>
    /// <param name="target">Цель, которую мы проверяем на возможность быть увиденной смотрящим</param>
    /// <returns>Да/нет</returns>
    public bool CanBeWatched(Entity<BlinkableComponent?> viewer, EntityUid target)
    {
        if (!Resolve(viewer.Owner, ref viewer.Comp, false))
            return false;

        if (viewer.Owner == target)
            return false;

        return true;
    }

    /// <summary>
    /// Проверка на то, может ли смотрящий видеть цель
    /// </summary>
    /// <param name="viewer">Смотрящий</param>
    /// <param name="target">Цель, которую проверяем</param>
    /// <param name="useFov">Применять ли проверку на поле зрения?</param>
    /// <param name="fovOverride">Если нужно использовать другой угол поля зрения</param>
    /// <returns>Видит ли смотрящий цель</returns>
    public bool IsEyeBlinded(Entity<BlinkableComponent?> viewer, EntityUid target, bool useFov = false, float? fovOverride = null)
    {
        if (_mobState.IsIncapacitated(viewer))
            return true;

        // Проверяем, видит ли смотрящий цель
        if (useFov & !IsInViewAngle(viewer, target, fovOverride ?? DefaultFieldOfViewAngle))
            return true; // Если не видит, то не считаем его как смотрящего

        if (_blinking.IsBlind(viewer, true))
            return true;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(viewer, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return true;

        return false;
    }

    /// <summary>
    /// Проверяет, находится ли цель в поле зрения
    /// </summary>
    /// <param name="viewer">Смотрящий</param>
    /// <param name="target">Цель, которую мы проверяем</param>
    /// <param name="maxAngle">Угол обзора в градусах</param>
    /// <returns>Находится ли цель в поле зрения</returns>
    public bool IsInViewAngle(EntityUid viewer, EntityUid target, float maxAngle)
    {
        var angle = FindAngleBetween(viewer, target);

        // Если angle больше, чем maxAngle -> цель вне поля зрения
        // Если меньше, значит в поле зрения
        return angle < maxAngle;
    }

    // TODO: Более подробно описать, что делает метод
    // После нескольких месяцев после написания этого кода нейронкой я забыл, что тут конкретно происходит
    // Но зато работает
    public float FindAngleBetween(Entity<TransformComponent?> viewer, Entity<TransformComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return float.MaxValue;

        if (!Resolve(viewer, ref viewer.Comp))
            return float.MaxValue;

        var targetWorldPosition = _transform.GetMoverCoordinates(target.Owner);
        var viewerWorldPosition = _transform.GetMoverCoordinates(viewer.Owner);

        var toTarget = (targetWorldPosition.Position - viewerWorldPosition.Position).Normalized(); // Вектор от target к SCP
        var viewerForward = viewer.Comp.LocalRotation.ToWorldVec(); // Направление взгляда target

        var dotProduct = Vector2.Dot(viewerForward, toTarget);

        // Если цель смотрит спиной, возвращаем MaxValue
        if (dotProduct < 0)
            return float.MaxValue;

        // Иначе вычисляем угол
        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
    }
}
