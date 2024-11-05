using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp173;

public abstract class SharedScp173System : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, ComponentInit>(OnInit);

        #region Blocker

        SubscribeLocalEvent((Entity<Scp173Component> _, ref BeforeDamageChangedEvent args) => args.Cancelled = true);
        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, component, args) =>
        {
            if (Is173Watched((uid, component), out _))
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
        if (Is173Watched(ent, out _))
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

        if (Is173Watched((ent, component), out _))
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

    protected void BreakNeck(EntityUid target, Scp173Component scp)
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

    protected bool Is173Watched(Entity<Scp173Component> scp173, out int watchersCount)
    {
        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(scp173).Coordinates, ExamineSystemShared.MaxRaycastRange);

        watchersCount = eyes
            .Where(eye => _examine.InRangeUnOccluded(eye, scp173, scp173.Comp.WatchRange, ignoreInsideBlocker: false))
            .Count(eye => !IsEyeBlinded(eye));

        return watchersCount != 0;
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
