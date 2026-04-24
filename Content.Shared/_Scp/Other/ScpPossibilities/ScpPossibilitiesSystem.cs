
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Mech;
using System.Linq;
using Content.Shared.Mech.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared._Scp.Other.ScpPossibilities;

public sealed partial class ScpPossibilitiesSystem : EntitySystem
{
    private EntityQuery<MechComponent> _mechComp;
    public override void Initialize()
    {
        base.Initialize();

        _mechComp = GetEntityQuery<MechComponent>();

        SubscribeLocalEvent<ScpPossibilitiesComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    public void OnAttackAttempt(Entity<ScpPossibilitiesComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target == null)
            return;

        if (!_mechComp.TryComp(args.Target.Value, out var _))
            return;

        if (!ent.Comp.CanEjectPilotFromMech)
            return;

        var ev = new MechEjectPilotEvent();
        RaiseLocalEvent(args.Target.Value, ev);
    }
}
