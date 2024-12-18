using Content.Shared.Bed.Sleep;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Scp.Pull;

public sealed class CanBePulledSleepingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CanBePulledSleepingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CanBePulledSleepingComponent, SleepStateChangedEvent>(OnSleepStateChangedEvent);
    }

    private void OnComponentInit(EntityUid uid, CanBePulledSleepingComponent component, ComponentInit args)
    {
        if (component.Exclusive && HasComp<PullableComponent>(uid))
            RemComp<PullableComponent>(uid);
    }

    private void OnSleepStateChangedEvent(EntityUid uid,
        CanBePulledSleepingComponent component,
        ref SleepStateChangedEvent args)
    {
        switch (args.FellAsleep)
        {
            case true:
                AddComp<PullableComponent>(uid);
                break;
            case false:
                RemComp<PullableComponent>(uid);
                break;
        }
    }
}
