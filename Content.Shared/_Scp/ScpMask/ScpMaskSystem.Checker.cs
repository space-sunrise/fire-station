using Content.Shared.Trigger;

namespace Content.Shared._Scp.ScpMask;

public sealed partial class ScpMaskSystem
{
    private void InitializeChecker()
    {
        SubscribeLocalEvent<ScpMaskCheckerComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<ScpMaskCheckerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!ent.Comp.BlockTriggers)
            return;

        if (!TryGetScpMask(ent, out var scpMask))
            return;

        TryCreatePopup(ent, scpMask);
        args.Cancelled = true;
    }
}
