﻿using System.Linq;
using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp173;

public sealed class Scp173System : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_net.IsServer)
        {
            SubscribeLocalEvent<Scp173Component, ComponentInit>(OnInit);
        }

        #region Blocker

        SubscribeLocalEvent((Entity<Scp173Component> _, ref BeforeDamageChangedEvent args) => args.Cancelled = true);
        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, _, args) =>
        {
            if (Is173Watched(uid))
                args.Cancel();
        });

        #endregion

        #region Movement

        SubscribeLocalEvent<Scp173Component, ChangeDirectionAttemptEvent>(OnDirectionAttempt);
        SubscribeLocalEvent<Scp173Component, MoveInputEvent>(OnInput);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);

        #endregion

        #region Abillities

        SubscribeLocalEvent<Scp173Component, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<Scp173Component, Scp173BlindAction>(OnBlind);
        SubscribeLocalEvent<Scp173Component, Scp173FastMovementAction>(OnFastMovement);

        #endregion

    }

    private void OnInit(Entity<Scp173Component> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent, "Scp173Blind");
        _actionsSystem.AddAction(ent, "Scp173Clog");
        _actionsSystem.AddAction(ent, "Scp173DamageStructure");
        _actionsSystem.AddAction(ent, "Scp173FastMovement");
    }

    #region Movement

    private void OnDirectionAttempt(Entity<Scp173Component> ent, ref ChangeDirectionAttemptEvent args)
    {
        if (Is173Watched(ent))
            args.Cancel();
    }

    private void OnInput(Entity<Scp173Component> ent, ref MoveInputEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _blocker.UpdateCanMove(ent);
    }

    private void OnMoveAttempt(EntityUid ent, Scp173Component component, UpdateCanMoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (Is173Watched(ent))
            args.Cancel();
    }

    #endregion

    #region Abillities

    private void OnMeleeHit(Entity<Scp173Component> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
           BreakNeck(entity);
        }
    }

    private void OnBlind(Entity<Scp173Component> ent, ref Scp173BlindAction args)
    {
        if (args.Handled)
            return;

        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(ent).Coordinates, ExamineSystemShared.MaxRaycastRange)
            .ToList();

        foreach (var eye in eyes)
        {
            _blinking.ForceBlind(eye.Owner, eye.Comp, TimeSpan.FromSeconds(6));
        }

        // TODO: Add sound.

        args.Handled = true;
    }

    private void OnFastMovement(Entity<Scp173Component> ent, ref Scp173FastMovementAction args)
    {
        if (args.Handled)
            return;

        var targetCords = args.Target.ToMap(EntityManager);
        var playerPos = _transform.GetWorldPosition(args.Performer);

        if (!_examine.InRangeUnOccluded(
                targetCords,
                _transform.GetMapCoordinates(args.Performer),
                ExamineSystemShared.MaxRaycastRange,
                null))
            return;

        var direction = targetCords.Position - playerPos;

        var distance = direction.Length();
        if (distance > ent.Comp.MaxRange)
        {
            direction = Vector2.Normalize(direction) * ent.Comp.MaxRange;
            targetCords = _transform.GetMapCoordinates(args.Performer).Offset(direction);
        }

        var normalizedDirection = Vector2.Normalize(direction);
        var ray = new CollisionRay(playerPos, normalizedDirection, collisionMask: (int)CollisionGroup.MobLayer);
        var rayCastResults = _physics.IntersectRay(targetCords.MapId, ray, direction.Length(), args.Performer, false);

        foreach (var eResult in rayCastResults)
        {
            var entity = eResult.HitEntity;
            BreakNeck(entity);
        }

        _transform.SetCoordinates(args.Performer, _transform.ToCoordinates(targetCords));

        args.Handled = true;
    }

    #endregion

    #region Helpers

    private bool Is173Watched(EntityUid scp173)
    {
        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(scp173).Coordinates, ExamineSystemShared.MaxRaycastRange)
            .ToList();

        return eyes.Count != 0 &&
               eyes.Where(eye => _examine.InRangeUnOccluded(eye, scp173, 12f, ignoreInsideBlocker:false))
                   .Any(eye => !IsEyeBlinded(eye));
    }

    private bool IsEyeBlinded(Entity<BlinkableComponent> eye)
    {
        if (_mobState.IsIncapacitated(eye))
            return true;

        if (_blinking.IsBlind(eye.Owner, eye.Comp))
            return true;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(eye, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return true;

        return false;
    }

    private void BreakNeck(EntityUid ent)
    {
        // Not a mob...
        if (!HasComp<MobStateComponent>(ent))
            return;

        // Not a human, right? Can`t broke his neck...
        if (!HasComp<HumanoidAppearanceComponent>(ent))
            return;

        // Already dead.
        if (_mobState.IsDead(ent))
            return;

        var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 100);
        _damageableSystem.TryChangeDamage(ent, damageSpec, true);

        _audio.PlayPvs(new SoundCollectionSpecifier("Scp173NeckSnap"), ent);

        _mobState.ChangeMobState(ent, MobState.Dead);
    }

    #endregion
}
