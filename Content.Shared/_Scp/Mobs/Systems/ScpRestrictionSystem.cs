using Content.Shared._Scp.Mobs.Components;
using Content.Shared._Scp.ScpMask;
using Content.Shared.Actions.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Events;
using Content.Shared.DragDrop;
using Content.Shared.Electrocution;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Slippery;

namespace Content.Shared._Scp.Mobs.Systems;

public sealed class ScpRestrictionSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRestrictionComponent, DisarmAttemptEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, ElectrocutionAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<ScpRestrictionComponent, TryingToSleepEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<ScpRestrictionComponent, BeingPulledAttemptEvent>(OnBeingPulled);
        SubscribeLocalEvent<ScpRestrictionComponent, SlipAttemptEvent>((_, _, args) => args.NoSlip = true);
        SubscribeLocalEvent<ScpRestrictionComponent, BuckleAttemptEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, CanDragEvent>((_, _, args) => args.Handled = false);
        SubscribeLocalEvent<ScpRestrictionComponent, BeforeStaminaDamageEvent>((_, _, args) => args.Cancelled = true);

    }

    private static void OnPullAttempt(EntityUid uid, ScpRestrictionComponent component, PullAttemptEvent args)
    {
        if (!component.CanPull)
            args.Cancelled = true;
    }

    private void OnBeingPulled(EntityUid uid, ScpRestrictionComponent component, BeingPulledAttemptEvent args)
    {
        var canBePulled = _mobState.IsIncapacitated(uid)
                          || HasComp<SleepingComponent>(uid)
                          || _scpMask.HasScpMask(uid)
                          || component.CanBePulled;

        if (!canBePulled)
            args.Cancel();
    }
}
