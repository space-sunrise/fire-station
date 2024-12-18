using Content.Server._Scp.Scp939;
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
                if (!HasComp<Scp939MuzzledComponent>(uid)) // костыль чтоб нельзя было нуллифицировать эффект маски при сне
                    RemComp<PullableComponent>(uid);
                break;
        }
    }
}
