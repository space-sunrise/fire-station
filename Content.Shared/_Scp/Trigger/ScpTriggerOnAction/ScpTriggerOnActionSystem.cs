using Content.Shared._Scp.Other.Events;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Content.Shared._Scp.ScpMask;

namespace Content.Shared._Scp.Trigger.ScpTriggerOnAction;

public sealed class ScpTriggerOnActionSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpTriggerOnActionComponent, AttemptTriggerEvent>(OnTriggerAttempt);
        SubscribeLocalEvent<ScpTriggerOnActionComponent, ScpActionTriggerAttemptEvent>(OnAction);
    }

    private void OnTriggerAttempt(Entity<ScpTriggerOnActionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!ent.Comp.CheckScpMask)
            return;

        if (args.Key == null)
            return;

        if (ent.Comp.KeyOut == null)
            return;

        if (ent.Comp.KeyOut != args.Key)
            return;

        if (_scpMask.TryGetScpMask(ent, out var scpMask))
        {
            _scpMask.TryCreatePopup(ent, scpMask);
            args.Cancelled = true;
            return;
        }
    }

    private void OnAction(Entity<ScpTriggerOnActionComponent> ent, ref ScpActionTriggerAttemptEvent args)
    {
        _trigger.Trigger(ent, ent, ent.Comp.KeyOut);

        args.Cancel();
    }
}
