
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;

namespace Content.Shared._Scp.Other.ScpPossibilities;

public sealed partial class ScpPossibilitiesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpPossibilitiesComponent, MeleeHitEvent>(OnAttackAttempt);
    }

    public void OnAttackAttempt(Entity<ScpPossibilitiesComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        if (!ent.Comp.CanEjectPilotFromMech)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!TryComp<MechComponent>(target, out var mechComp))
                continue;

            if (mechComp.PilotSlot.ContainedEntities == null)
                continue;

            var ev = new MechEjectPilotEvent();
            RaiseLocalEvent(target, ev);
        }
    }
}
