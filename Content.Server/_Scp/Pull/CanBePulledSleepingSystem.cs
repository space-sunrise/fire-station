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

    private void OnComponentInit(Entity<CanBePulledSleepingComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Exclusive)
            RemComp<PullableComponent>(ent);
    }

    private void OnSleepStateChangedEvent(Entity<CanBePulledSleepingComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
        {
            AddComp<PullableComponent>(ent);
        }
        else if (!HasComp<Scp939MuzzledComponent>(ent)) // костыль чтоб нельзя было нуллифицировать эффект маски при сне
        {
            RemComp<PullableComponent>(ent);
        }
    }
}
