using System.Linq;
using System.Numerics;
using Content.Server.Doors.Systems;
using Content.Server.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp173;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Light.Components;
using Content.Shared.Lock;
using Content.Shared.Mobs.Components;
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
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private readonly SoundSpecifier _storageOpenSound = new SoundCollectionSpecifier("MetalBreak");
    private readonly SoundSpecifier _clogSound = new SoundPathSpecifier("/Audio/_Scp/Scp173/clog.ogg");

    private const float ToggleDoorStuffChance = 0.35f;

    private TimeSpan _nextReagentCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, MapInitEvent>(OnInit);

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

    private void OnInit(Entity<Scp173Component> ent, ref MapInitEvent args)
    {
        // Выставляем безопасное время
        ent.Comp.SafeTimeEnd = _gameTicker.RoundStartTimeSpan + ent.Comp.SafeTime;
        Dirty(ent);
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

        if (!IsInSafeTime(ent, predicted: false))
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
            _explosion.QueueExplosion(_transform.GetMapCoordinates(ent), ExplosionSystem.DefaultExplosionPrototypeId, 300f, 0.6f, 50f, ent);
        }

        args.Handled = true;
    }

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

    private (bool isValid, MapCoordinates coords) ValidateAndCalculateTarget(Scp173FastMovementAction args, Scp173Component component)
    {
        var targetCoords = _transform.ToMapCoordinates(args.Target);
        var performerCoords = _transform.GetMapCoordinates(args.Performer);
        var performerPos = _transform.GetWorldPosition(args.Performer);

        if (!_examine.InRangeUnOccluded(
            targetCoords,
            performerCoords,
            ExamineSystemShared.MaxRaycastRange,
            null))
        {
            return (false, default);
        }

        var direction = targetCoords.Position - performerPos;
        var distance = direction.Length();

        if (distance > component.MaxJumpRange)
        {
            direction = Vector2.Normalize(direction) * component.MaxJumpRange;
            targetCoords = performerCoords.Offset(direction);
        }

        return (true, targetCoords);
    }

    private EntityCoordinates? CalculateFinalPosition(Entity<Scp173Component> scpEntitiy, MapCoordinates targetCoords)
    {
        var performerPos = _transform.GetWorldPosition(scpEntitiy);
        var direction = targetCoords.Position - performerPos;
        var normalizedDirection = Vector2.Normalize(direction);

        var ray = new CollisionRay(
            performerPos,
            normalizedDirection,
            collisionMask: (int)CollisionGroup.AllMask
        );

        var rayCastResults = _physics.IntersectRay(
                targetCoords.MapId,
                ray,
                direction.Length(),
                scpEntitiy,
                false
            )
            .OrderBy(x => x.Distance)
            .ToList();

        var previousHitPos = performerPos;
        foreach (var result in rayCastResults)
        {
            if (CanBreakNeck(result.HitEntity))
            {
                BreakNeck(result.HitEntity, scpEntitiy);
                previousHitPos = result.HitPos;

                var worldPos = _transform.GetWorldPosition(scpEntitiy);
                _transform.SetWorldPosition(scpEntitiy, worldPos);

                continue;
            }

            if (IsImpassableObstacle(result.HitEntity))
            {
                var potentialPosition = result.HitPos;
                if (HasEnoughSpace(potentialPosition, scpEntitiy, targetCoords.MapId))
                {
                    return _transform.ToCoordinates(new MapCoordinates(potentialPosition, targetCoords.MapId));
                }

                var safePosition = FindSafePosition(previousHitPos, result.HitPos, scpEntitiy, targetCoords.MapId);
                if (safePosition != null)
                {
                    return null;
                }

                return _transform.ToCoordinates(new MapCoordinates(performerPos, targetCoords.MapId));
            }

            previousHitPos = result.HitPos;
        }

        if (HasEnoughSpace(targetCoords.Position, scpEntitiy, targetCoords.MapId))
        {
            return _transform.ToCoordinates(targetCoords);
        }

        var safePositionMaybe = FindSafePosition(performerPos, targetCoords.Position, scpEntitiy, targetCoords.MapId);

        if (safePositionMaybe.HasValue)
        {
            return _transform.ToCoordinates(new MapCoordinates(safePositionMaybe.Value, targetCoords.MapId));
        }

        return null;
    }

    private bool HasEnoughSpace(Vector2 position, EntityUid entityUid, MapId mapId)
    {
        var fixtureComponent = Comp<FixturesComponent>(entityUid);
        var fixture = fixtureComponent.Fixtures.Values.First();

        var transform = new Transform(position, 0);
        var halfSize = fixture.Shape.ComputeAABB(transform, 0).Extents;
        var testBox = new Box2(position - halfSize, position + halfSize);

        var query = _physics.GetCollidingEntities(mapId, testBox);

        foreach (var collidingEntity in query)
        {
            if (IsImpassableObstacle(collidingEntity.Owner))
                return false;
        }

        return true;
    }

    private Vector2? FindSafePosition(Vector2 start, Vector2 end, EntityUid entityUid, MapId mapId)
    {
        const int maxAttempts = 10;
        const float stepBack = 0.25f;

        var direction = end - start;
        var normalizedDirection = Vector2.Normalize(direction);

        for (var i = 1; i <= maxAttempts; i++)
        {
            var testPosition = end - (normalizedDirection * (stepBack * i));
            if (HasEnoughSpace(testPosition, entityUid, mapId))
            {
                return testPosition;
            }
        }

        return null;
    }

    private bool CanBreakNeck(EntityUid entity)
    {
        return HasComp<MobStateComponent>(entity);
    }

    private bool IsImpassableObstacle(EntityUid entity)
    {
        if (!TryComp<PhysicsComponent>(entity, out var collidedEntityPhysics))
            return false;

        if (!collidedEntityPhysics.Hard)
        {
            return false;
        }

        var layer = (CollisionGroup)collidedEntityPhysics.CollisionLayer;

        return layer.HasFlag(CollisionGroup.WallLayer) || layer.HasFlag(CollisionGroup.TableLayer);
    }
}
