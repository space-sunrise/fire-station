using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Electrocution;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Slippery;

namespace Content.Shared._Scp.Mobs.Systems;

public sealed class ScpRestrictionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRestrictionComponent, DisarmAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<ScpRestrictionComponent, ElectrocutionAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<ScpRestrictionComponent, TryingToSleepEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, PullAttemptEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, BeingPulledAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<ScpRestrictionComponent, SlipAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<ScpRestrictionComponent, BuckleAttemptEvent>((_, _, args) => args.Cancelled = true);
        SubscribeLocalEvent<ScpRestrictionComponent, CanDragEvent>((_, _, args) => args.Handled = false);
        SubscribeLocalEvent<ScpRestrictionComponent, BeforeStaminaDamageEvent>((_, _, args) => args.Cancelled = true);
    }
}
