using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Pulling.Events;

namespace Content.Shared._Scp.ScpMask;

public sealed partial class ScpMaskSystem
{
    private void InitializeRestrictions()
    {
        SubscribeLocalEvent<ScpComponent, AttemptStopPullingEvent>(OnStopPullingAttempt);
        SubscribeLocalEvent<ScpComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ScpComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnStopPullingAttempt(Entity<ScpComponent> ent, ref AttemptStopPullingEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.User != ent)
            return;

        if (!TryGetScpMask(ent, out var mask))
            return;

        if (!mask.Value.Comp.BlockStopPulling)
            return;

        var message = Loc.GetString("scp-mask-block-stop-pulling", ("mask", Name(mask.Value)));
        _popup.PopupClient(message, ent, ent);

        args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<ScpComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        if (!TryGetScpMask(ent, out var mask))
            return;

        if (!mask.Value.Comp.BlockAttacks)
            return;

        var message = Loc.GetString("scp-mask-block-attacks" , ("mask", Name(mask.Value)));
        _popup.PopupClient(message, ent, ent);

        args.Cancel();
    }

    private void OnPickupAttempt(Entity<ScpComponent> ent, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryGetScpMask(ent, out var mask))
            return;

        if (!mask.Value.Comp.BlockPickups)
            return;

        var message = Loc.GetString("scp-mask-block-pickups", ("mask", Name(mask.Value)));
        _popup.PopupClient(message, ent, ent);

        args.Cancel();
    }
}
