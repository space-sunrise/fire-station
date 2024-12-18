using Content.Shared.Clothing;
using Content.Shared.Item.ItemToggle;

namespace Content.Server._Scp.Clothing;

public sealed class ClothingToggleOnEquipSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingToggleOnEquipComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClothingToggleOnEquipComponent, ClothingGotUnequippedEvent>(OnUneqipped);
    }

    private void OnEquipped(EntityUid uid, ClothingToggleOnEquipComponent component, ref ClothingGotEquippedEvent args)
    {
        _toggle.Toggle(uid);
    }

    private void OnUneqipped(EntityUid uid, ClothingToggleOnEquipComponent component, ref ClothingGotUnequippedEvent args)
    {
        _toggle.Toggle(uid);
    }
}
