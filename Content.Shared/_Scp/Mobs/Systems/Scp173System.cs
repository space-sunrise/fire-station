using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Network;

namespace Content.Shared._Scp.Mobs.Systems;

public sealed class Scp173System : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, MoveInputEvent>(OnInput);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);
    }

    private void OnInput(Entity<Scp173Component> ent, ref MoveInputEvent args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnMoveAttempt(EntityUid ent, Scp173Component component, UpdateCanMoveEvent args)
    {
        if (Is173Watched(ent))
            args.Cancel();
    }

    private bool Is173Watched(EntityUid scp173)
    {
        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(scp173).Coordinates,
            ExamineSystemShared.MaxRaycastRange)
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
}
