using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Storage.Components;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<InsideEntityStorageComponent> _insideStorageQuery;

    private void InitializeApi()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _insideStorageQuery = GetEntityQuery<InsideEntityStorageComponent>();
    }

    public bool IsWatched(EntityUid ent, bool useFov = true, float? fovOverride = null)
    {
        return IsWatched(ent, out _, useFov, fovOverride);
    }

    public bool IsWatched(EntityUid ent, out int watchersCount, bool useFov = true, float? fovOverride = null)
    {
        watchersCount = 0;
        var potentialWatchers = RentBlinkableList();
        var searchSet = RentBlinkableSet();

        var result = IsWatched(ent, potentialWatchers, searchSet, useFov , fovOverride);
        watchersCount = potentialWatchers.Count;

        ReturnBlinkableList(potentialWatchers);
        ReturnBlinkableSet(searchSet);

        return result;
    }

    /// <summary>
    /// Проверяет, смотрит ли кто-то на указанную цель
    /// </summary>
    /// <param name="ent">Цель, которую проверяем</param>
    /// <param name="watchersCount">Количество смотрящих</param>
    /// <param name="useFov">Нужно ли проверять поле зрения</param>
    /// <param name="fovOverride">Если нужно использовать другой угол обзора, отличный от стандартного</param>
    /// <returns>Смотрит ли на цель хоть кто-то</returns>
    public bool IsWatched(EntityUid ent,
        List<Entity<BlinkableComponent>> potentialWatchers,
        HashSet<Entity<BlinkableComponent>> searchSet,
        bool useFov = true,
        float? fovOverride = null)
    {
        if (!TryGetAllEntitiesVisibleTo(ent, potentialWatchers, searchSet))
            return false;

        return IsWatchedBy(ent, potentialWatchers , useFov, fovOverride);
    }

    public bool TryGetAllEntitiesVisibleTo(
        Entity<TransformComponent?> ent,
        List<Entity<BlinkableComponent>> potentialWatchers,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.All)
    {
        var searchSet = RentBlinkableSet();
        var result = TryGetAllEntitiesVisibleTo(ent, potentialWatchers, searchSet, type, flags);
        ReturnBlinkableSet(searchSet);

        return result;
    }

    /// <summary>
    /// Получает и возвращает всех потенциально смотрящих на указанную цель.
    /// </summary>
    /// <remarks>
    /// В методе нет проверок на дополнительные состояния, такие как моргание/закрыты ли глаза/поле зрения т.п.
    /// Единственная проверка - можно ли физически увидеть цель(т.е. не закрыта ли она стеной и т.п.)
    /// </remarks>
    /// <param name="ent">Цель, для которой ищем потенциальных смотрящих</param>
    /// <param name="potentialWatchers">Список всех, кто потенциально видит цель</param>
    /// <param name="type">Требуемая прозрачность линии видимости.</param>
    /// <param name="targets">Заранее заготовленный список, который будет использоваться в <see cref="EntityLookupSystem"/></param>
    /// <param name="flags">Список флагов для поиска целей в <see cref="EntityLookupSystem"/></param>
    /// <returns>Удалось ли найти хоть кого-то</returns>
    public bool TryGetAllEntitiesVisibleTo<T>(
        Entity<TransformComponent?> ent,
        List<Entity<T>> potentialWatchers,
        HashSet<Entity<T>> searchSet,
        LineOfSightBlockerLevel type = LineOfSightBlockerLevel.Transparent,
        LookupFlags flags = LookupFlags.All)
        where T : IComponent
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        searchSet.Clear();
        _lookup.GetEntitiesInRange(ent.Comp.Coordinates, SeeRange, searchSet, flags);

        foreach (var target in searchSet)
        {
            if (target.Owner == ent.Owner)
                continue;

            if (!_proximity.IsRightType(ent, target, type, out _))
                continue;

            potentialWatchers.Add(target);
        }

        return potentialWatchers.Count != 0;
    }

    /// <summary>
    /// Проверяет, смотрят ли переданные сущности на указанную цель. Передает список всех сущностей, что действительно смотрят на цель
    /// </summary>
    /// <param name="target">Цель</param>
    /// <param name="potentialViewers">Список сущностей для проверки</param>
    /// <param name="realViewers">Список всех сущностей, что действительно смотрят на цель</param>
    /// <param name="useFov">Нужно ли проверять, находится ли цель в поле зрения сущности</param>
    /// <param name="fovOverride">Если нужно перезаписать угол поля зрения</param>
    /// <returns>Смотрит ли хоть кто-то на цель</returns>
    public bool IsWatchedBy(EntityUid target,
        List<Entity<BlinkableComponent>> potentialViewers,
        List<Entity<BlinkableComponent>> realViewers,
        bool useFov = true,
        float? fovOverride = null)
    {
        foreach (var viewer in potentialViewers)
        {
            if (!CanBeWatched(viewer.AsNullable(), target))
                continue;

            if (IsEyeBlinded(viewer.AsNullable(), target, useFov, fovOverride))
                continue;

            realViewers.Add(viewer);
        }

        return realViewers.Any();
    }

    public bool IsWatchedBy(EntityUid target,
        List<Entity<BlinkableComponent>> potentialViewers,
        bool useFov = true,
        float? fovOverride = null)
    {
        foreach (var viewer in potentialViewers)
        {
            if (!CanBeWatched(viewer.AsNullable(), target))
                continue;

            if (IsEyeBlinded(viewer.AsNullable(), target, useFov, fovOverride))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Простая проверка на то, видят ли переданную сущность другие сущности.
    /// Вместо проверки на интервальное моргание используется проверка на мануальное закрытие глаз.
    /// </summary>
    /// <param name="target">Сущность, на которую смотрят</param>
    /// <param name="potentialViewers">Смотрящие</param>
    /// <returns>Смотри ли хоть кто-нибудь из переданных</returns>
    public bool SimpleIsWatchedBy(EntityUid target, List<EntityUid> potentialViewers)
    {
        foreach (var viewer in potentialViewers)
        {
            if (!SimpleIsWatchedBy(target, viewer))
                continue;

            return true;
        }

        return false;
    }

    public bool SimpleIsWatchedBy(EntityUid target, EntityUid potentialViewer)
    {
        if (!CanBeWatched(potentialViewer, target))
            return false;

        if (_blinking.AreEyesClosedManually(potentialViewer))
            return false;

        return true;
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

        if (_insideStorageQuery.HasComp(viewer))
            return false;

        if (_mobStateQuery.TryComp(viewer, out var mobState) && _mobState.IsIncapacitated(viewer, mobState))
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
    public bool IsEyeBlinded(Entity<BlinkableComponent?> viewer, EntityUid target, bool useFov = true, float? fovOverride = null)
    {
        if (_mobState.IsIncapacitated(viewer))
            return true;

        // Проверяем, видит ли смотрящий цель
        if (useFov && !_fov.IsInFov(viewer.Owner, target, fovOverride))
            return true; // Если не видит, то не считаем его как смотрящего

        if (_blinking.IsBlind(viewer, true))
            return true;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(viewer, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return true;

        return false;
    }
}
