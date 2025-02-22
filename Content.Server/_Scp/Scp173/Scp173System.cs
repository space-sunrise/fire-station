﻿using System.Linq;
using System.Numerics;
using Content.Server.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Interaction;
using Content.Server.Light.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp173;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Lock;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp173;

public sealed class Scp173System : SharedScp173System
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio= default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SoundSpecifier _storageOpenSound = new SoundCollectionSpecifier("MetalBreak");
    private readonly SoundSpecifier _clogSound = new SoundPathSpecifier("/Audio/_Scp/Scp173/clog.ogg");
    private const float ToggleDoorStuffChance = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173DamageStructureAction>(OnStructureDamage);
        SubscribeLocalEvent<Scp173Component, Scp173ClogAction>(OnClog);
        SubscribeLocalEvent<Scp173Component, Scp173FastMovementAction>(OnFastMovement);
    }

    protected override void BreakNeck(EntityUid target, Scp173Component scp)
    {
        if (!HasComp<MobStateComponent>(target))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (_mobState.IsDead(target))
            return;

        if (scp.NeckSnapDamage == null)
            return;

        _damageable.TryChangeDamage(target, scp.NeckSnapDamage, true, useVariance:false);

        // TODO: Fix missing deathgasp emote on high damage per once

        _audio.PlayPvs(scp.NeckSnapSound, target);
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

        if (Is173Watched(uid, out _))
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

        var defileRadius = 4f;

        var lookup = _lookup.GetEntitiesInRange(uid, defileRadius)
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

        if (Is173Watched(ent, out _))
        {
            var message = Loc.GetString("scp173-fast-movement-too-many-watchers");
            _popup.PopupEntity(message, ent, ent, PopupType.LargeCaution);
            return;
        }

        var coords = Transform(ent).Coordinates;

        var tempSol = new Solution();
        tempSol.AddReagent(ent.Comp.Reagent, 25);
        _puddle.TrySpillAt(coords, tempSol, out _, false);

        _audio.PlayPvs(_clogSound, ent);

        FixedPoint2 total = 0;
        var puddles = _lookup.GetEntitiesInRange<PuddleComponent>(coords, ContainmentRoomSearchRadius)
            .Where(puddle =>
                _interaction.InRangeUnobstructed(ent.Owner, puddle.Owner, ExamineSystemShared.ExamineRange));

        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototype);
            total = allReagents
                .Where(reagent => reagent.Key.ID == "Scp173Reagent")
                .Aggregate(total, (current, reagent) => current + reagent.Value);
        }

        if (total >= ent.Comp.MinTotalSolutionVolume)
        {
            var transform = Transform(args.Performer);
            var lookup = _lookup.GetEntitiesInRange(transform.Coordinates, 5, flags: LookupFlags.Dynamic | LookupFlags.Static)
                .Where(target => _interaction.InRangeUnobstructed(ent.Owner, target, ExamineSystemShared.ExamineRange));

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
        else if (total >= ent.Comp.ExtraMinTotalSolutionVolume)
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

        if (Is173Watched(ent, out var watchersCount) && watchersCount > ent.Comp.MaxWatchers)
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

                var mobXform = Transform(result.HitEntity);
                _transform.SetWorldPosition(scpEntitiy, mobXform.WorldPosition);

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
