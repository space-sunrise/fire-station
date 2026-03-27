using Content.Shared.Trigger;

namespace Content.Shared._Scp.Trigger.TriggerOnSignalSwitch;

public sealed class TriggerOnSignalSwitchSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSignalSwitchComponent, SignalSwitchActivatedEvent>(OnSignalSwitchActivated);
    }

    private void OnSignalSwitchActivated(Entity<TriggerOnSignalSwitchComponent> ent, ref SignalSwitchActivatedEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Mode == SignalSwitchTriggerMode.ActivatedOnly && !args.Activated)
            return;

        Trigger.Trigger(ent, args.User, ent.Comp.KeyOut);
    }
}
