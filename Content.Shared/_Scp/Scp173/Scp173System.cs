using System.Linq;
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
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, ComponentInit>(OnInit);

        #region Blocker

        SubscribeLocalEvent((Entity<Scp173Component> _, ref BeforeDamageChangedEvent args) => args.Cancelled = true);
        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, component, args) =>
        {
            if (Is173Watched((uid, component)))
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
        // Fallback
        ent.Comp.NeckSnapDamage ??= new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 200);

        Dirty(ent);
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

        if (Is173Watched((ent, component)))
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
           BreakNeck(entity, ent.Comp);
        }
    }

    private void OnBlind(Entity<Scp173Component> ent, ref Scp173BlindAction args)
    {
        if (args.Handled)
            return;

        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(ent).Coordinates, ExamineSystemShared.MaxRaycastRange);

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
        if (distance > ent.Comp.MaxJumpRange)
        {
            direction = Vector2.Normalize(direction) * ent.Comp.MaxJumpRange;
            targetCords = _transform.GetMapCoordinates(args.Performer).Offset(direction);
        }

        var normalizedDirection = Vector2.Normalize(direction);
        var ray = new CollisionRay(playerPos, normalizedDirection, collisionMask: (int)CollisionGroup.MobLayer);
        var rayCastResults = _physics.IntersectRay(targetCords.MapId, ray, direction.Length(), args.Performer, false);

        foreach (var eResult in rayCastResults)
        {
            var entity = eResult.HitEntity;
            BreakNeck(entity, ent.Comp);
        }

        _transform.SetCoordinates(args.Performer, _transform.ToCoordinates(targetCords));

        args.Handled = true;
    }

    private void BreakNeck(EntityUid target, Scp173Component scp)
    {
        // Not a mob...
        if (!HasComp<MobStateComponent>(target))
            return;

        // Not a human, right? Can`t broke his neck...
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        // Already dead.
        if (_mobState.IsDead(target))
            return;

        // No damage??
        if (scp.NeckSnapDamage == null)
            return;

        _damageableSystem.TryChangeDamage(target, scp.NeckSnapDamage, true, applyRandomDamage: false);

        // TODO: Fix missing deathgasp emote on high damage per once

        _audio.PlayPvs(scp.NeckSnapSound, target);
    }

    #endregion

    #region Helpers

    private bool Is173Watched(Entity<Scp173Component> scp173)
    {
        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(scp173).Coordinates, ExamineSystemShared.MaxRaycastRange);

        return eyes.Count != 0 &&
               eyes.Where(eye => _examine.InRangeUnOccluded(eye, scp173, scp173.Comp.WatchRange, ignoreInsideBlocker: false))
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

    #endregion
}
