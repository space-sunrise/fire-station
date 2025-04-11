using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp106.Systems;

public sealed partial class Scp106System
{
    private void OnShop(EntityUid uid, Scp106Component component, Scp106ShopAction args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    protected override void ToggleBlade(Entity<Scp106Component> ent, EntProtoId blade)
    {
        base.ToggleBlade(ent, blade);

        // Если клинок уже имеется
        if (ent.Comp.HandTransformed)
        {
            HideBlade(ent);
        }
        else
        {
            EnsureComp<HandsComponent>(ent);
            _hands.AddHand(ent, "right", HandLocation.Middle);
            var sword = Spawn(blade, Transform(ent).Coordinates);

            ent.Comp.Sword = sword;
            _hands.TryPickup(ent, sword, "right");

            ent.Comp.HandTransformed = true;
        }
    }

    public void HideBlade(Entity<Scp106Component> ent)
    {
        if (!Exists(ent.Comp.Sword))
            return;

        Del(ent.Comp.Sword);
        _hands.RemoveHands(ent);
        ent.Comp.HandTransformed = false;
    }
}
