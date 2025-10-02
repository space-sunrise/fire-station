using Content.Shared.Item;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private void InitializeHands()
    {
        SubscribeLocalEvent<Scp096Component, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<Scp096Component> ent, ref PickupAttemptEvent args)
    {
        // Если все ок - ничего не делаем и выходим.
        if (!_whitelist.IsBlacklistPass(ent.Comp.PickupBlacklist, args.Item))
            return;

        // Если все ок - ничего не делаем и выходим.
        if (_whitelist.IsWhitelistPass(ent.Comp.PickupWhitelist, args.Item))
            return;

        var message = Loc.GetString("scp096-cant-pickup", ("name", Name(args.Item)));
        _popup.PopupClient(message, args.Item, ent);
        args.Cancel();
    }
}
