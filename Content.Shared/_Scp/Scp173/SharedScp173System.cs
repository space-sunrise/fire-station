using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

public abstract class SharedScp173System : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<Scp173Component, MoveInputEvent>(OnInput);

        #endregion

        #region Abillities

        SubscribeLocalEvent<Scp173Component, StartCollideEvent>(OnCollide);

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

    private void OnMoveAttempt(Entity<Scp173Component> ent, ref UpdateCanMoveEvent args)
    {
        if (Is173Watched(ent, out _))
            args.Cancel();
    }

    private void OnInput(Entity<Scp173Component> ent, ref MoveInputEvent args)
    {
        _blocker.UpdateCanMove(ent);
    }

    #endregion

    #region Abillities

    private void OnCollide(Entity<Scp173Component> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!_blinking.IsBlind(target, useTimeCompensation: true))
            return;

        BreakNeck(target, ent.Comp);
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

    protected abstract void BreakNeck(EntityUid target, Scp173Component scp);

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

        if (_blinking.IsBlind(eye.Owner, eye.Comp, true))
            return true;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(eye, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return true;

        return false;
    }

    #endregion
}
