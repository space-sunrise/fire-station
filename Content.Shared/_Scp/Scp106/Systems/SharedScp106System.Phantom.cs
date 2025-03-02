using Content.Shared._Scp.Scp106.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Robust.Shared.Physics;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System
{
    private void OnScp106ReverseAction(EntityUid uid, Scp106PhantomComponent component, Scp106ReverseAction args)
    {
        var doAfter = new DoAfterArgs(EntityManager,
            uid,
            TimeSpan.FromSeconds(3),
            new Scp106ReverseActionEvent(),
            eventTarget: uid,
            target: args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnScp106LeavePhantomAction(Entity<Scp106PhantomComponent> ent, ref Scp106LeavePhantomAction args)
    {
        _mob.ChangeMobState(ent, MobState.Dead);
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

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            ent,
            TimeSpan.FromSeconds(4),
            new Scp106PassThroughActionEvent(),
            ent)
        {
            BreakOnDropItem = false,
            BreakOnMove = false,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnWeightlessMove = false,
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnScp106PassThroughActionEvent(Entity<Scp106PhantomComponent> ent, ref Scp106PassThroughActionEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(ent, id, fixture, (int) CollisionGroup.SmallMobMask);
            _physics.SetCollisionLayer(ent, id, fixture, (int) CollisionGroup.MobLayer);
        }
    }
}
