using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Proximity;

/// <summary>
/// Единая монолитная система, которая будет вызывать ивенты
/// при приближении сущности с <see cref="ProximityTargetComponent"/> к сущности с <see cref="ProximityReceiverComponent"/>.
/// Ивенты вызываются на обе сущности, что позволяет создавать логику при разных ситуациях.
/// </summary>
/// <remarks>
/// Система позволяет настраивать требуемый уровень видимости между сущностями, расстояние.
/// Настройка происходит в <see cref="ProximityTargetComponent"/>.
/// </remarks>
public sealed class ProximitySystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan ProximitySearchCooldown = TimeSpan.FromSeconds(0.3f);
    private static TimeSpan _nextSearchTime = TimeSpan.Zero;

    // Оптимизации аллокации памяти
    private static HashSet<Entity<ProximityTargetComponent>> _targets = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _nextSearchTime = TimeSpan.Zero);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // Оптимизации, чтобы просчет не происходил часто
        if (_timing.CurTime < _nextSearchTime)
            return;

        var query = EntityQueryEnumerator<ProximityReceiverComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var receiver, out var xform))
        {
            _targets.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, receiver.CloseRange, _targets);

            foreach (var target in _targets)
            {
                if (!IsRightType(uid, target, receiver.RequiredLineOfSight, out var lightOfSightBlockerLevel))
                    continue;

                var targetCoords = Transform(target).Coordinates;

                if (!xform.Coordinates.TryDistance(EntityManager, _transform, targetCoords, out var range))
                    continue;

                var receiverEvent = new ProximityInRangeReceiverEvent(target, range, receiver.CloseRange, lightOfSightBlockerLevel);
                RaiseLocalEvent(uid, receiverEvent);

                var targetEvent = new ProximityInRangeTargetEvent(uid, range, receiver.CloseRange, lightOfSightBlockerLevel);
                RaiseLocalEvent(target, targetEvent);
            }
        }

        _nextSearchTime = _timing.CurTime + ProximitySearchCooldown;
    }

    /// <summary>
    /// Проверяет, совпадает ли тип прозрачности сущностей между двумя переданными сущностям.
    /// </summary>
    /// <param name="receiver">Первая сущность</param>
    /// <param name="target">Вторая сущность</param>
    /// <param name="type">Нужный тип</param>
    /// <param name="level">Текущий тип перекрытия между сущностями</param>
    /// <returns>Совпадает или нет</returns>
    public bool IsRightType(EntityUid receiver, EntityUid target, LineOfSightBlockerLevel type, out LineOfSightBlockerLevel level)
    {
        level = GetLightOfSightBlockerLevel(receiver, target);
        return level <= type;
    }

    /// <summary>
    /// Получает тип заслоения сущности в зависимости от того, что препятствует(или нет) их прямому контакту.
    /// Например, если <see cref="receiver"/> находится за окном от <see cref="target"/>, то метод выдаст <see cref="LineOfSightBlockerLevel.Transparent"/>.
    /// </summary>
    /// <param name="receiver">Первая сущность</param>
    /// <param name="target">Вторая сущность</param>
    /// <returns>Тип прозрачности сущностей, перекрывающий прямой контакт между этими двумя</returns>
    public LineOfSightBlockerLevel GetLightOfSightBlockerLevel(EntityUid receiver, EntityUid target)
    {
        var isUnobstructed = _interaction.InRangeUnobstructed(receiver, target, float.MaxValue);
        var isUnOccluded = _examine.InRangeUnOccluded(receiver, target, float.MaxValue);

        if (isUnOccluded && isUnobstructed)
            return LineOfSightBlockerLevel.None;
        else if (!isUnobstructed && isUnOccluded)
            return LineOfSightBlockerLevel.Transparent;
        else
            return LineOfSightBlockerLevel.Solid;
    }
}
