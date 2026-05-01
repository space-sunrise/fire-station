using System.Text.Json;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._Scp.MetaGarbage;

public sealed class MetaGarbageCartridgeSystem : EntitySystem
{
    private const string CartridgeSpentKey = "cartridge.spent";
    private const string HitScanCartridgeSpentKey = "hitscan_cartridge.spent";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CartridgeAmmoComponent, MetaGarbageSaveEvent>(OnCartridgeSave);
        SubscribeLocalEvent<CartridgeAmmoComponent, MetaGarbageRestoreEvent>(OnCartridgeRestore);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, MetaGarbageSaveEvent>(OnHitScanSave);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, MetaGarbageRestoreEvent>(OnHitScanRestore);
    }

    private void OnCartridgeSave(Entity<CartridgeAmmoComponent> ent, ref MetaGarbageSaveEvent args)
    {
        args.ExtraData[CartridgeSpentKey] = JsonSerializer.SerializeToElement(ent.Comp.Spent);
    }

    private void OnCartridgeRestore(Entity<CartridgeAmmoComponent> ent, ref MetaGarbageRestoreEvent args)
    {
        if (args.ExtraData.TryGetValue(CartridgeSpentKey, out var val)
            && val.ValueKind == JsonValueKind.True)
        {
            ent.Comp.Spent = true;
            Dirty(ent);
        }
    }

    private void OnHitScanSave(Entity<HitScanCartridgeAmmoComponent> ent, ref MetaGarbageSaveEvent args)
    {
        args.ExtraData[HitScanCartridgeSpentKey] = JsonSerializer.SerializeToElement(ent.Comp.Spent);
    }

    private void OnHitScanRestore(Entity<HitScanCartridgeAmmoComponent> ent, ref MetaGarbageRestoreEvent args)
    {
        if (args.ExtraData.TryGetValue(HitScanCartridgeSpentKey, out var val)
            && val.ValueKind == JsonValueKind.True)
        {
            ent.Comp.Spent = true;
            Dirty(ent);
        }
    }
}
