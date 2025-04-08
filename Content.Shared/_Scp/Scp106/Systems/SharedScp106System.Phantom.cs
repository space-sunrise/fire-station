using Content.Shared._Scp.Scp106.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Robust.Shared.Physics;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System
{
    private void OnScp106ReverseAction(Entity<Scp106PhantomComponent> ent, ref Scp106ReverseAction args)
    {
        if (args.Handled)
            return;

        if (!_mob.IsDead(args.Target))
            return;

        var doAfter = new DoAfterArgs(EntityManager, ent, args.Delay, new Scp106ReverseActionEvent(), eventTarget: ent, target: args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnScp106LeavePhantomAction(Entity<Scp106PhantomComponent> ent, ref Scp106LeavePhantomAction args)
    {
        if (args.Handled)
            return;

        _mob.ChangeMobState(ent, MobState.Dead);
        args.Handled = true;
    }

    private void OnScp106PassThroughAction(Entity<Scp106PhantomComponent> ent, ref Scp106PassThroughAction args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(ent, id, fixture, (int) CollisionGroup.GhostImpassable);
            _physics.SetCollisionLayer(ent, id, fixture, (int) CollisionGroup.GhostImpassable);
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(args.Delay), new Scp106PassThroughActionEvent(),ent)
        {
            BreakOnDropItem = false,
            BreakOnMove = false,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnWeightlessMove = false,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnScp106PassThroughActionEvent(Entity<Scp106PhantomComponent> ent, ref Scp106PassThroughActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(ent, id, fixture, (int) CollisionGroup.SmallMobMask);
            _physics.SetCollisionLayer(ent, id, fixture, (int) CollisionGroup.MobLayer);
        }

        args.Handled = true;
    }
}
