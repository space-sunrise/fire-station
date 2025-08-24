using Content.Server.Hands.Systems;
using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp106.Systems;

public sealed partial class Scp106System
{
    [Dependency] private readonly HandsSystem _hands = default!;

    public override bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args)
    {
        if (args.Args.EventTarget is not {} phantom)
            return false;

        if (!TryComp<Scp106PhantomComponent>(phantom, out var phantomComponent))
            return false;

        if (!_mind.TryGetMind(phantom, out var mindId, out _))
            return false;

        var scp106 = phantomComponent.Scp106BodyUid;

        if (!Exists(scp106))
            return false;

        if (!TryComp<Scp106Component>(scp106, out var scp106Component))
            return false;

        _mind.TransferTo(mindId, scp106);

        var phantomPos = Transform(phantom).Coordinates;

        _transform.SetCoordinates(scp106.Value, phantomPos);

        Del(phantom);

        Scp106FinishTeleportation(scp106.Value, scp106Component.TeleportationDuration);

        return true;
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
            _hands.AddHand(ent.Owner, "right", HandLocation.Middle);
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
        ent.Comp.Sword = null;
        _hands.RemoveHands(ent.Owner);
        ent.Comp.HandTransformed = false;
    }
}
