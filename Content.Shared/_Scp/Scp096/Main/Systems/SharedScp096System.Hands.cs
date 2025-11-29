using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Item;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private void InitializeHands()
    {
        SubscribeLocalEvent<Scp096Component, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<Scp096Component> ent, ref PickupAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.PickupBlacklist, args.Item))
        {
            CancelPickup(ent, ref args);
            return;
        }

        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.PickupWhitelist, args.Item))
        {
            CancelPickup(ent, ref args);
            return;
        }

        if (_standing.IsDown(ent.Owner))
        {
            CancelPickup(ent, ref args);
            return;
        }
    }

    private void CancelPickup(EntityUid ent, ref PickupAttemptEvent args)
    {
        var message = Loc.GetString("scp096-cant-pickup", ("name", Name(args.Item)));
        _popup.PopupClient(message, args.Item, ent);
        args.Cancel();
    }
}
