using Robust.Shared.Timing;

namespace Content.Shared._Scp.Watching;

/// <summary>
/// Единая система, обрабатывающая смотрение игроков друг на друга.
/// Включает различные проверки, например поле зрения, закрыты ли глаза и подобное
/// </summary>
public sealed partial class EyeWatchingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan WatchingCheckInterval = TimeSpan.FromSeconds(0.2f);

    public override void Initialize()
    {
        SubscribeLocalEvent<WatchingTargetComponent, MapInitEvent>(OnMapInit);
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

            // Проверяет всех потенциальных смотрящих на то, действительно ли они видят цель.
            // Каждый потенциально смотрящий проходит полный комплекс проверок.
            // Выдает полный список всех сущностей, кто действительно видит цель
            if (!IsWatchedBy(uid, potentialViewers, viewers: out var viewers, false))
                continue;

            // Вызываем ивент на смотрящем, говорящем, что он видит цель
            foreach (var viewer in viewers)
            {
                var firstTime = !watchingComponent.AlreadyLookedAt.ContainsKey(viewer);

                // За подробностями какой ивент для чего навести мышку на название ивента
                RaiseLocalEvent(viewer, new EntityLookedAtEvent((uid, watchingComponent), firstTime));
                RaiseLocalEvent(uid, new EntitySeenEvent(viewer, firstTime));

                // Добавляет смотрящего в список уже смотревших, чтобы позволить системам манипулировать этим
                // И предотвращать эффект, если игрок смотрит не первый раз или не так давно
                watchingComponent.AlreadyLookedAt[viewer] = _timing.CurTime;
                Dirty(uid, watchingComponent);
            }

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
public sealed class EntityLookedAtEvent(Entity<WatchingTargetComponent> target, bool firstTime) : EntityEventArgs
{
    public readonly Entity<WatchingTargetComponent> Target = target;
    public readonly bool IsSeenFirstTime = firstTime;
}

/// <summary>
/// Ивент вызываемый на цели, передающий информации, что на нее кто-то посмотрел
/// </summary>
/// <param name="viewer">Смотрящий, который увидел цель</param>
/// <param name="firstTime">Видим ли мы цель в первый раз</param>
public sealed class EntitySeenEvent(EntityUid viewer, bool firstTime) : EntityEventArgs
{
    public readonly EntityUid Viewer = viewer;
    public readonly bool IsSeenFirstTime = firstTime;
}
