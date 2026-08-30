using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Other.ItemEquipWhitelist;

public sealed class ItemEquipWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemEquipWhitelistComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
    }

    private void OnBeingEquippedAttempt(Entity<ItemEquipWhitelistComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if ((args.SlotFlags & ent.Comp.TargetSlots) == 0)
            return;

        if (!_whitelist.CheckBoth(args.Equipee, ent.Comp.Blacklist, ent.Comp.Whitelist))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString(ent.Comp.Reason), args.Equipee, args.Equipee, PopupType.SmallCaution);
        }
    }
}
