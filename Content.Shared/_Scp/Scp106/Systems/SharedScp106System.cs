using System.Linq;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.DoAfter;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract class SharedScp106System : EntitySystem
{
    // TODO: SOUNDING, EFFECTS.

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsAction>(OnBackroomsAction);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportAction>(OnRandomTeleportAction);

        SubscribeLocalEvent<Scp106Component, Scp106BackroomsActionEvent>(OnBackroomsDoAfter);
        SubscribeLocalEvent<Scp106Component, Scp106RandomTeleportActionEvent>(OnTeleportDoAfter);
    }

    private void OnBackroomsAction(Entity<Scp106Component> ent, ref Scp106BackroomsAction args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106BackroomsActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };

        if (_doAfter.TryStartDoAfter(doAfterEventArgs))
            args.Handled = true;
    }

    private void OnRandomTeleportAction(Entity<Scp106Component> ent, ref Scp106RandomTeleportAction args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(5), new Scp106RandomTeleportActionEvent(), args.Performer, args.Performer)
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };

        if (_doAfter.TryStartDoAfter(doAfterEventArgs))
            args.Handled = true;
    }

    private void OnBackroomsDoAfter(Entity<Scp106Component> ent, ref Scp106BackroomsActionEvent args)
    {
        if (args.Cancelled)
            return;

        SendToBackrooms(args.User);
    }

    private void OnTeleportDoAfter(Entity<Scp106Component> ent, ref Scp106RandomTeleportActionEvent args)
    {
        if (args.Cancelled)
            return;

        SendToStation(ent);
    }

    private void OnMeleeHit(Entity<Scp106Component> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
            if (entity == ent.Owner)
                return;

            SendToBackrooms(entity);
        }
    }

    public virtual async void SendToBackrooms(EntityUid target) {}

    public virtual void SendToStation(EntityUid target) {}
}
