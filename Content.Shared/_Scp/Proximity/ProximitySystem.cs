using System.Linq;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan ProximitySearchCooldown = TimeSpan.FromSeconds(0.05f);
    private static TimeSpan _nextSearchTime = TimeSpan.Zero;

    // Оптимизации аллокации памяти
    private static readonly HashSet<Entity<ProximityTargetComponent>> Targets = [];
    private static readonly HashSet<EntityUid> PossibleNotInRange = [];
    private static readonly HashSet<EntityUid> AllTargets = [];

    private const float JustUselessNumber = 30f;

    /// <summary>
    /// Список тегов, которые обозначают непрозрачный объект-преграду.
    /// </summary>
    private static readonly HashSet<ProtoId<TagPrototype>> SolidTags =
    [
        "Wall",
        "Window",
        "Airlock",
        "GlassAirlock",
        "HighSecDoor",
        "Windoor",
        "Directional",
        "SecureWindoor",
        "SecurePlasmaWindoor",
        "SecureUraniumWindoor",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Clean());

        SubscribeLocalEvent<ProximityTargetComponent, ComponentStartup>(AddToTargets);
        SubscribeLocalEvent<ProximityTargetComponent, ComponentShutdown>(RemoveFromTargets);
        SubscribeLocalEvent<ProximityTargetComponent, EntityTerminatingEvent>(RemoveFromTargets);
    }

    private static void Clean()
    {
        _nextSearchTime = TimeSpan.Zero;
        AllTargets.Clear();
    }

    #region All targets population

    private static void AddToTargets<T>(Entity<ProximityTargetComponent> ent, ref T args) => AllTargets.Add(ent);
    private static void RemoveFromTargets<T>(Entity<ProximityTargetComponent> ent, ref T args) => AllTargets.Remove(ent);

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // Оптимизации, чтобы просчет не происходил часто
        if (_timing.CurTime < _nextSearchTime)
            return;

        PossibleNotInRange.Clear();
        PossibleNotInRange.UnionWith(AllTargets);

        var query = EntityQueryEnumerator<ProximityReceiverComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var receiver, out var xform))
        {
            Targets.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, receiver.CloseRange, Targets);

            foreach (var target in Targets)
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

                PossibleNotInRange.Remove(target);
            }
        }

        foreach (var target in PossibleNotInRange)
        {
            var notInRangeEvent = new ProximityNotInRangeTargetEvent();
            RaiseLocalEvent(target, ref notInRangeEvent);
        }

        _nextSearchTime = _timing.CurTime + ProximitySearchCooldown;
    }


    /// <inheritdoc cref="IsRightType(EntityUid, EntityUid, LineOfSightBlockerLevel, out LineOfSightBlockerLevel)"/>
    public bool IsRightType(EntityUid receiver, EntityUid target, LineOfSightBlockerLevel type)
    {
        return IsRightType(receiver, target, type, out _);
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
        if (HasComp<InsideEntityStorageComponent>(receiver))
            return LineOfSightBlockerLevel.Solid;

        var isUnOccluded = _examine.InRangeUnOccluded(receiver, target, JustUselessNumber);

        if (!isUnOccluded)
            return LineOfSightBlockerLevel.Solid;
        else if (!InRangeUnobstructed(receiver, target))
            return LineOfSightBlockerLevel.Transparent;
        else
            return LineOfSightBlockerLevel.None;
    }

    private bool InRangeUnobstructed(Entity<TransformComponent?> first, Entity<TransformComponent?> second)
    {
        return _interaction.InRangeUnobstructed(
            first,
            second,
            JustUselessNumber,
            predicate: IsNotSolidObject);
    }

    private bool IsNotSolidObject(EntityUid e) => !_tag.HasAnyTag(e, SolidTags);

    /// <summary>
    /// Проверяет, есть ли в заданном радиусе сущность с компонентом T
    /// </summary>
    /// <param name="uid">Сущность, от которой берем радиус</param>
    /// <param name="range">Радиус проверки</param>
    /// <param name="level"><see cref="LineOfSightBlockerLevel"/></param>
    /// <typeparam name="T">Компонент, который должен быть у искомой сущности</typeparam>
    /// <returns>Имеется ли рядом такая сущность или нет</returns>
    public bool IsNearby<T>(EntityUid uid, float range, LineOfSightBlockerLevel level = LineOfSightBlockerLevel.None) where T : IComponent
    {
        return _lookup.GetEntitiesInRange<T>(Transform(uid).Coordinates, range)
            .Any(e => IsRightType(uid, e, level, out _));
    }

    /// <summary>
    /// Проверяет, есть ли в заданном радиусе сущность с компонентом T
    /// </summary>
    /// <param name="uid">Сущность, от которой берем радиус</param>
    /// <param name="range">Радиус проверки</param>
    /// <param name="buffer">Заготовленный список, чтобы не плодить каждый раз новый</param>
    /// <param name="level"><see cref="LineOfSightBlockerLevel"/></param>
    /// <typeparam name="T">Компонент, который должен быть у искомой сущности</typeparam>
    /// <returns>Имеется ли рядом такая сущность или нет</returns>
    public bool IsNearby<T>(EntityUid uid, float range, HashSet<Entity<T>> buffer, LineOfSightBlockerLevel level = LineOfSightBlockerLevel.None) where T : IComponent
    {
        _lookup.GetEntitiesInRange(Transform(uid).Coordinates, range, buffer);

        return buffer.Any(e => IsRightType(uid, e, level, out _));
    }
}
