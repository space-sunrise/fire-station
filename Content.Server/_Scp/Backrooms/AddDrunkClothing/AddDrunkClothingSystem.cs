using Content.Shared.Clothing;
using Content.Shared.Drunk;

namespace Content.Server._Scp.Backrooms.AddDrunkClothing;

public sealed class AddDrunkClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddDrunkClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddDrunkClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<AddDrunkClothingComponent> entity, ref ClothingGotEquippedEvent args)
    {
        if (HasComp<DrunkComponent>(args.Wearer))
            return;

        _drunkSystem.TryApplyDrunkenness(args.Wearer, int.MaxValue);

        entity.Comp.IsActive = true;
    }

    private void OnGotUnequipped(Entity<AddDrunkClothingComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        if (!entity.Comp.IsActive)
            return;

        _drunkSystem.TryRemoveDrunkenness(args.Wearer);

        entity.Comp.IsActive = false;
    }
}
