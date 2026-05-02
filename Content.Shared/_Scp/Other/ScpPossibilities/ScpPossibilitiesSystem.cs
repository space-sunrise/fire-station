
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Other.ScpPossibilities;

public sealed class ScpPossibilitiesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpPossibilitiesComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public void OnMeleeHit(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.CanEjectPilotFromMech)
            return;

        if (args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!TryComp<MechComponent>(target, out var mechComp))
                continue;

            if (mechComp.PilotSlot.ContainedEntity == null)
                continue;

            var ev = new MechEjectPilotEvent();
            RaiseLocalEvent(target, ev);
        }
    }
}
