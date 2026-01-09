using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Scp012;

public abstract class SharedScp012System : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp012Component, GettingPickedUpAttemptEvent>(OnGettingPickedUp);
    }

    private void OnGettingPickedUp(Entity<Scp012Component> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (_whitelist.CheckBoth(args.User, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var message = Loc.GetString("scp012-failed-pickup");
        _popup.PopupClient(message, args.User, args.User);

        args.Cancel();
    }
}
