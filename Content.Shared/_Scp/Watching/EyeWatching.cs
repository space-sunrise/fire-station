using Content.Shared._Scp.Proximity;
using Content.Shared.Mobs.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Watching;

/// <summary>
/// Единая система, обрабатывающая смотрение игроков друг на друга.
/// Включает различные проверки, например поле зрения, закрыты ли глаза и подобное
/// </summary>
public sealed partial class EyeWatchingSystem : EntitySystem
{
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan WatchingCheckInterval = TimeSpan.FromSeconds(0.3f);

    public const float SeeRange = 16f;

    public override void Initialize()
    {
        SubscribeLocalEvent<WatchingTargetComponent, MapInitEvent>(OnMapInit);

        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _insideStorageQuery = GetEntityQuery<InsideEntityStorageComponent>();
    }

    private void OnMapInit(Entity<WatchingTargetComponent> ent, ref MapInitEvent args)
    {
        SetNextTime(ent);
    }

    /// <summary>
    /// Обрабатывает все сущности, помеченные как цель для просмотра. Вызывает ивент на смотрящем, если он видит цель.
    /// Это может использоваться для создания различных эффектов или динамических проверок
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<WatchingTargetComponent>();
        while (query.MoveNext(out var uid, out var watchingComponent))
        {
            if (_timing.CurTime < watchingComponent.NextTimeWatchedCheck)
                continue;

            // Все потенциально возможные смотрящие. Среди них те, что прошли фаст-чек из самых простых проверок
            var potentialViewers = GetWatchers(uid);

            // Вызываем ивенты на потенциально смотрящих. Без особых проверок
            // Полезно в коде, который уже использует подобные проверки или не требует этого
            foreach (var potentialViewer in potentialViewers)
            {
                // За подробностями какой ивент для чего навести мышку на название ивента
                RaiseLocalEvent(potentialViewer, new SimpleEntityLookedAtEvent((uid, watchingComponent)));
                RaiseLocalEvent(uid, new SimpleEntitySeenEvent(potentialViewer));
            }

            // Проверяет всех потенциальных смотрящих на то, действительно ли они видят цель.
            // Каждый потенциально смотрящий проходит полный комплекс проверок.
            // Выдает полный список всех сущностей, кто действительно видит цель
            if (!IsWatchedBy(uid, potentialViewers, viewers: out var viewers))
                continue;

            // Вызываем ивент на смотрящем, говорящие, что он действительно видит цель
            foreach (var viewer in viewers)
            {
                var netViewer = GetNetEntity(viewer);
                var firstTime = !watchingComponent.AlreadyLookedAt.ContainsKey(netViewer);
                var blockerLevel = _proximity.GetLightOfSightBlockerLevel(viewer, uid);

                // Небольшая заглушка для удобства работы с ивентами.
                // Использовать firstTime не очень удобно, поэтому в качестве дополнительного способа определения будет TimeSpan.Zero
                watchingComponent.AlreadyLookedAt[netViewer] = TimeSpan.Zero;

                // За подробностями какой ивент для чего навести мышку на название ивента
                RaiseLocalEvent(viewer, new EntityLookedAtEvent((uid, watchingComponent), firstTime, blockerLevel));
                RaiseLocalEvent(uid, new EntitySeenEvent(viewer, firstTime, blockerLevel));

                // Добавляет смотрящего в список уже смотревших, чтобы позволить системам манипулировать этим
                // И предотвращать эффект, если игрок смотрит не первый раз или не так давно
                watchingComponent.AlreadyLookedAt[netViewer] = _timing.CurTime;
            }

            Dirty(uid, watchingComponent);
            SetNextTime(watchingComponent);
        }
    }

    private void SetNextTime(WatchingTargetComponent component)
    {
        component.NextTimeWatchedCheck = _timing.CurTime + WatchingCheckInterval;
    }
}

/// <summary>
/// Ивент вызываемый на смотрящем, передающий информации, что он посмотрел на кого-то
/// </summary>
/// <param name="target">Цель, на которую посмотрели</param>
/// <param name="firstTime">Видим ли мы цель в первый раз</param>
/// <param name="blockerLevel">Линия видимости между смотрящим и целью, подробнее <see cref="LineOfSightBlockerLevel"/></param>
public sealed class EntityLookedAtEvent(Entity<WatchingTargetComponent> target, bool firstTime, LineOfSightBlockerLevel blockerLevel) : EntityEventArgs
{
    public readonly Entity<WatchingTargetComponent> Target = target;
    public readonly bool IsSeenFirstTime = firstTime;
    public readonly LineOfSightBlockerLevel BlockerLevel = blockerLevel;
}

/// <summary>
/// Ивент вызываемый на цели, передающий информации, что на нее кто-то посмотрел
/// </summary>
/// <param name="viewer">Смотрящий, который увидел цель</param>
/// <param name="firstTime">Видим ли мы цель в первый раз</param>
/// <param name="blockerLevel">Линия видимости между смотрящим и целью, подробнее <see cref="LineOfSightBlockerLevel"/></param>
public sealed class EntitySeenEvent(EntityUid viewer, bool firstTime, LineOfSightBlockerLevel blockerLevel) : EntityEventArgs
{
    public readonly EntityUid Viewer = viewer;
    public readonly bool IsSeenFirstTime = firstTime;
    public readonly LineOfSightBlockerLevel BlockerLevel = blockerLevel;
}

/// <summary>
/// Простой ивент, говорящий, что смотрящий посмотрел на цель.
/// Вызывается до прохождения различных проверок на смотрящем. Если вдруг требуются собственная ручная проверка
/// </summary>
/// <param name="target">Цель, на которую посмотри</param>
public sealed class SimpleEntityLookedAtEvent(Entity<WatchingTargetComponent> target) : EntityEventArgs
{
    public readonly Entity<WatchingTargetComponent> Target = target;
}

/// <summary>
/// Простой ивент, говорящий, что на цель кто-то посмотрел.
/// Вызывается до прохождения различных проверок на цели. Если вдруг требуются собственная ручная проверка
/// </summary>
/// <param name="viewer">Смотрящий</param>
public sealed class SimpleEntitySeenEvent(EntityUid viewer) : EntityEventArgs
{
    public readonly EntityUid Viewer = viewer;
}
