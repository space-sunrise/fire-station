using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Scp.Other.ScpPossibilities;

public abstract partial class SharedScpPossibilitiesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpPossibilitiesComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public virtual void OnMeleeHit(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (ent.Comp.CanEjectPilotFromMech)
            TryEjectFromMech(ref args);
    }

    public void TryEjectFromMech(ref MeleeHitEvent args)
    {
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
