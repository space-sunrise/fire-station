using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Pulling.Events;

namespace Content.Shared._Scp.ScpMask;

public sealed partial class ScpMaskSystem
{
    private void InitializeRestrictions()
    {
        SubscribeLocalEvent<ScpComponent, AttemptStopPullingEvent>(OnStopPullingAttempt);
        SubscribeLocalEvent<ScpComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnStopPullingAttempt(Entity<ScpComponent> ent, ref AttemptStopPullingEvent args)
    {
        if (!TryGetScpMask(ent, out var mask))
            return;

        if (mask.Value.Comp.BlockStopPulling)
            args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<ScpComponent> ent, ref AttackAttemptEvent args)
    {
        if (!TryGetScpMask(ent, out var mask))
            return;

        if (mask.Value.Comp.BlockAttacks)
            args.Cancel();
    }
}
