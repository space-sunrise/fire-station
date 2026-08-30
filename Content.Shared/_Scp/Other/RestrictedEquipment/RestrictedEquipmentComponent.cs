using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.RestrictedEquipment;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RestrictedEquipmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField, AutoNetworkedField]
    public SlotFlags TargetSlots = SlotFlags.All & ~SlotFlags.POCKET;

    [DataField, AutoNetworkedField]
    public LocId Reason = "inventory-component-can-equip-does-not-fit";
}
