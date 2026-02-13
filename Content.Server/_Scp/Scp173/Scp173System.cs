using System.Linq;
using System.Numerics;
using Content.Server.Doors.Systems;
using Content.Server.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Other.DamageOnCollide;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp173;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Lock;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp173;

public sealed partial class Scp173System : SharedScp173System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio= default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ScpHelpers _helpers = default!;
    [Dependency] private readonly ScpDamageOnCollideSystem _damageOnCollide = default!;

    private readonly SoundSpecifier _storageOpenSound = new SoundCollectionSpecifier("MetalBreak");
    private readonly SoundSpecifier _clogSound = new SoundPathSpecifier("/Audio/_Scp/Scp173/clog.ogg");

    private const float ToggleDoorStuffChance = 0.35f;

    private TimeSpan _nextReagentCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173DamageStructureAction>(OnStructureDamage);
        SubscribeLocalEvent<Scp173Component, Scp173ClogAction>(OnClog);
        SubscribeLocalEvent<Scp173Component, Scp173FastMovementAction>(OnFastMovement);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Timing.CurTime < _nextReagentCheck)
            return;

        _nextReagentCheck = Timing.CurTime + ReagentCheckInterval;

        var query = EntityQueryEnumerator<Scp173Component>();
        while (query.MoveNext(out var uid, out var scp173))
        {
            if (!IsContained(uid))
                continue;

            scp173.ReagentVolumeAround = _helpers.GetAroundSolutionVolume(uid, Scp173Component.Reagent, LineOfSightBlockerLevel.None);
            Dirty(uid, scp173);
        }
    }

    private void OnStructureDamage(Entity<Scp173Component> uid, ref Scp173DamageStructureAction args)
    {
        if (args.Handled)
            return;

        if (IsInScpCage(uid, out var storage))
        {
            var message = Loc.GetString("scp-cage-suppress-ability", ("container", Name(storage.Value)));
            _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);

            return;
        }

        if (Watching.IsWatched(uid.Owner))
        {
            var message = Loc.GetString("scp173-fast-movement-too-many-watchers");
            _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);

            return;
        }

        if (IsContained(uid))
        {
            var message = Loc.GetString("scp173-damage-structures-blocked");
            _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);

            return;
        }

        var lookup = _lookup.GetEntitiesInRange(uid, 4f)
            .Where(ent => _interaction.InRangeUnobstructed(uid.Owner, ent, ExamineSystemShared.ExamineRange));

        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();
        var boltedDoors = GetEntityQuery<DoorBoltComponent>();
        var lockedStuff = GetEntityQuery<LockComponent>();
        var doors = GetEntityQuery<DoorComponent>();

        foreach (var ent in lookup)
        {
            // Наносим случайным вещам структурный дамаг
            var dspec = new DamageSpecifier();
            var damageValue = _random.Next(20, 80);
            dspec.DamageDict.Add("Structural", damageValue);
            _damageable.TryChangeDamage(ent, dspec);

            // Заставляем лампочки моргать
            if (lights.HasComp(ent))
                _ghost.DoGhostBooEvent(ent);

            // Снимаем болты
            if (_random.Prob(ToggleDoorStuffChance) && boltedDoors.TryComp(ent, out var doorBoltComp) && doorBoltComp.BoltsDown)
                _door.SetBoltsDown((ent, doorBoltComp), false, predicted: true);

            // Открываем шлюзы
            if (_random.Prob(ToggleDoorStuffChance) && doors.TryComp(ent, out var doorComp) && doorComp.State is not DoorState.Open)
                _door.StartOpening(ent);

            // Кешируем шанс, так как его придется использовать несколько раз
            var unlockProb = _random.Prob(ToggleDoorStuffChance);

            // Если шанс сработал и замок закрыт, то мы открываем замок.
            // Если ШАНС НЕ сработал, но замок есть и закрыт, то мы ничего не делаем
            // Иначе закрытые контейнеры станут открытыми, но замок останется закрыт и их невозможно будет закрыть, и исправить замок
            if (unlockProb && lockedStuff.TryComp(ent, out var lockComp) && lockComp.Locked)
                _lock.Unlock(ent, args.Performer, lockComp);
            else if (!unlockProb && lockedStuff.TryComp(ent, out var lockComp1) && lockComp1.Locked)
                continue;  // Нельзя открывать контейнеры без открытия замка, иначе он потом не закроется

            // Открываем шкафы и подобные хранилища. Так как проверка на замок уже есть можно не беспокоиться
            if (entityStorage.TryComp(ent, out var entityStorageComponent) && !entityStorageComponent.Open)
            {
                _entityStorage.OpenStorage(ent, entityStorageComponent);
                _audio.PlayPvs(_storageOpenSound, ent);
            }
        }

        // TODO: Sound.

        args.Handled = true;
    }

    private void OnClog(Entity<Scp173Component> ent, ref Scp173ClogAction args)
    {
        if (args.Handled)
            return;

        if (IsInScpCage(ent, out var storage))
        {
            var message = Loc.GetString("scp-cage-suppress-ability", ("container", Name(storage.Value)));
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);

            return;
        }

        if (Watching.IsWatched(ent.Owner))
        {
            var message = Loc.GetString("scp173-fast-movement-too-many-watchers");
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);
            return;
        }

        var coords = Transform(ent).Coordinates;

        var tempSol = new Solution();
        tempSol.AddReagent(Scp173Component.Reagent, 25);
        _puddle.TrySpillAt(coords, tempSol, out _, false);

        _audio.PlayPvs(_clogSound, ent);

        var total = _helpers.GetAroundSolutionVolume(ent, Scp173Component.Reagent, LineOfSightBlockerLevel.None);

        if (total >= Scp173Component.MinTotalSolutionVolume)
        {
            var lookup = _lookup.GetEntitiesInRange(coords, ContainmentRoomSearchRadius, flags: LookupFlags.Dynamic | LookupFlags.Static)
                .Where(target => _examine.InRangeUnOccluded(ent, target, ContainmentRoomSearchRadius));

            foreach (var target in lookup)
            {
                if (TryComp<DoorBoltComponent>(target, out var doorBoltComp) && doorBoltComp.BoltsDown)
                    _door.SetBoltsDown((target, doorBoltComp), false, predicted: true);

                if (TryComp<LockComponent>(target, out var lockComp) && lockComp.Locked)
                    _lock.Unlock(target, args.Performer, lockComp);

                if (TryComp<DoorComponent>(target, out var doorComp) && doorComp.State is not DoorState.Open)
                    _door.StartOpening(target);
            }
        }
        if (total >= Scp173Component.ExtraMinTotalSolutionVolume)
        {
            _explosion.QueueExplosion(_transform.GetMapCoordinates(ent), SharedExplosionSystem.DefaultExplosionPrototypeId, 300f, 0.6f, 50f, ent);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Обработчик способности быстрого перемещения (прыжка) SCP-173.
    /// Проверяет все условия, запрещающие прыжок, рассчитывает конечную позицию
    /// и перемещает статую, убивая всех мобов на пути.
    /// </summary>
    private void OnFastMovement(Entity<Scp173Component> ent, ref Scp173FastMovementAction args)
    {
        if (args.Handled)
            return;

        if (IsInScpCage(ent, out var storage))
        {
            var message = Loc.GetString("scp-cage-suppress-ability", ("container", Name(storage.Value)));
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);

            return;
        }

        if (IsContained(ent))
        {
            var message = Loc.GetString("scp173-damage-structures-blocked");
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);

            return;
        }

        if (Watching.IsWatched(ent.Owner, out var watchersCount) && watchersCount > ent.Comp.MaxWatchers)
        {
            var message = Loc.GetString("scp173-fast-movement-too-many-watchers");
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);

            return;
        }

        var (isValidTarget, targetCoords) = ValidateAndCalculateTarget(args, ent.Comp);
        if (!isValidTarget)
            return;

        var finalPosition = CalculateFinalPosition(ent, targetCoords);
        if (finalPosition == null)
            return;

        _transform.SetCoordinates(args.Performer, finalPosition.Value.SnapToGrid());

        _audio.PlayPvs(ent.Comp.TeleportationSound, ent, AudioParams.Default);
        args.Handled = true;
    }

    /// <summary>
    /// Проверяет, что цель прыжка находится в зоне видимости и в пределах допустимой дальности.
    /// Если цель слишком далеко, ограничивает дальность прыжка до <see cref="Scp173Component.MaxJumpRange"/>.
    /// Возвращает кортеж: (валидна ли цель, координаты цели на карте).
    /// </summary>
    private (bool isValid, MapCoordinates coords) ValidateAndCalculateTarget(Scp173FastMovementAction args, Scp173Component component)
    {
        var targetCoords = _transform.ToMapCoordinates(args.Target);
        var performerCoords = _transform.GetMapCoordinates(args.Performer);

        if (!_examine.InRangeUnOccluded(
            targetCoords,
            performerCoords,
            ExamineSystemShared.MaxRaycastRange,
            null))
        {
            return (false, default);
        }

        var direction = targetCoords.Position - performerCoords.Position;
        var distance = direction.Length();

        if (distance > component.MaxJumpRange)
        {
            direction = Vector2.Normalize(direction) * component.MaxJumpRange;
            targetCoords = performerCoords.Offset(direction);
        }

        return (true, targetCoords);
    }

    /// <summary>
    /// Рассчитывает конечную позицию SCP-173 после прыжка.
    /// Выполняет рейкаст от текущей позиции до цели. Все мобы на пути — убиваются.
    /// При столкновении с непроходимым препятствием (стена, стол) — SCP-173 останавливается
    /// на позиции последнего убитого моба, либо остаётся на месте, если убийств не было.
    /// Если путь свободен от препятствий — возвращает позицию цели.
    /// </summary>
    private EntityCoordinates? CalculateFinalPosition(Entity<Scp173Component> scp, MapCoordinates targetCoords)
    {
        var performerPos = _transform.GetWorldPosition(scp);
        var direction = targetCoords.Position - performerPos;
        var normalizedDirection = Vector2.Normalize(direction);

        var ray = new CollisionRay(
            performerPos,
            normalizedDirection,
            collisionMask: (int) CollisionGroup.AllMask
        );

        var rayCastResults = _physics.IntersectRay(
                targetCoords.MapId,
                ray,
                direction.Length(),
                scp,
                false
            )
            .OrderBy(x => x.Distance)
            .ToList();

        // Последняя безопасная позиция, на которой SCP-173 может оказаться.
        // Изначально — текущая позиция статуи.
        var lastSafePos = performerPos;

        foreach (var result in rayCastResults)
        {
            // Мобы на пути — убиваем и запоминаем позицию как безопасную
            if (!_damageOnCollide.TryApplyDamage(scp.Owner, result.HitEntity, requireVelocity: false))
            {
                lastSafePos = result.HitPos;
                continue;
            }

            // Непроходимое препятствие — останавливаемся на последней безопасной позиции
            if (IsImpassableObstacle(result.HitEntity))
            {
                return _transform.ToCoordinates(new MapCoordinates(lastSafePos, targetCoords.MapId));
            }
        }

        // Путь полностью свободен — летим до точки назначения
        return _transform.ToCoordinates(targetCoords);
    }

    /// <summary>
    /// Проверяет, является ли сущность непроходимым препятствием (стена или стол).
    /// Используется для определения, нужно ли остановить прыжок SCP-173 при столкновении.
    /// </summary>
    private bool IsImpassableObstacle(EntityUid entity)
    {
        if (!TryComp<PhysicsComponent>(entity, out var physics))
            return false;

        if (!physics.Hard)
            return false;

        var layer = (CollisionGroup) physics.CollisionLayer;

        return layer.HasFlag(CollisionGroup.WallLayer) || layer.HasFlag(CollisionGroup.TableLayer);
    }
}
