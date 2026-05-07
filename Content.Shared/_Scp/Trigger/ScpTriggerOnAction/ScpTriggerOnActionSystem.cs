using Content.Shared._Scp.Other.Events;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Scp.Trigger.ScpTriggerOnAction;

public sealed class ScpTriggerOnActionSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpTriggerOnActionListenerComponent, ScpTriggerOnActionEvent>(OnAction);
    }

    private void OnAction(Entity<ScpTriggerOnActionListenerComponent> ent, ref ScpTriggerOnActionEvent args)
    {
        if (_trigger.Trigger(ent, ent, args.KeyOut))
            args.Handled = true;
    }
}
