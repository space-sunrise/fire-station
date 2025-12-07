using System.Numerics;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private void InitializeHands()
    {
        SubscribeLocalEvent<Scp096Component, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<Scp096Component, DidEquipHandEvent>(OnEquipHand);

        Log.Level = LogLevel.Info;
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

    private void OnEquipHand(Entity<Scp096Component> ent, ref DidEquipHandEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.PickupBlacklist, args.Equipped))
        {
            PopupAndDropEntity(ent, args.Equipped);
            return;
        }

        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.PickupWhitelist, args.Equipped))
        {
            PopupAndDropEntity(ent, args.Equipped);
            return;
        }
    }

    private void CancelPickup(EntityUid ent, ref PickupAttemptEvent args)
    {
        var message = Loc.GetString("scp096-cant-pickup", ("name", Name(args.Item)));
        _popup.PopupClient(message, args.Item, ent);
        args.Cancel();
    }

    private void PopupAndDropEntity(EntityUid target, EntityUid item)
    {
        _hands.TryDrop(target, item, checkActionBlocker: false);

        var x = _random.NextFloatForEntity(item, -1f);
        var y = _random.NextFloatForEntity(target, -1f);

        _throwing.TryThrow(item, new Vector2(x, y));
    }
}
