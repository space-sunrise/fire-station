using Content.Shared._Scp.Scp999;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server._Scp.Scp999;

// TODO: Uncopypaste state changing code
public sealed partial class Scp999System
{
    private void FromDefaultToWall(Entity<Scp999Component> ent)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        var fix2 = _fixture.GetFixtureOrNull(ent, WallFixtureId, fixturesComponent);
        if (fix2 == null)
            return;

        var attemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Wall);
        RaiseLocalEvent(ent, attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        ent.Comp.CurrentState = Scp999States.Wall;
        Dirty(ent);

        var xform = Transform(ent);
        _transform.AnchorEntity(ent, xform);

        // shitcode
        _physics.TrySetBodyType(ent, BodyType.Dynamic, fixturesComponent, physicsComponent, xform);
        _physics.SetCollisionLayer(ent, WallFixtureId, fix2, 221);
        _physics.SetCollisionMask(ent, WallFixtureId, fix2, 158);

        EnsureComp<NoRotateOnInteractComponent>(ent);
        EnsureComp<NoRotateOnMoveComponent>(ent);

        _audio.PlayPvs(ent.Comp.WallSound, ent);

        var toWallChangedEvent = new Scp999ChangedStateEvent(Scp999States.Wall);
        RaiseLocalEvent(ent, toWallChangedEvent);

        var ev = new Scp999WallifyEvent(GetNetEntity(ent), ent.Comp.States[Scp999States.Wall]);
        RaiseNetworkEvent(ev);
    }

    private void FromWallToDefault(Entity<Scp999Component> ent)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        var fix2 = _fixture.GetFixtureOrNull(ent, WallFixtureId, fixturesComponent);
        if (fix2 == null)
            return;

        var attemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Default);
        RaiseLocalEvent(ent, attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        ent.Comp.CurrentState = Scp999States.Default;
        Dirty(ent);

        var xform = Transform(ent);
        _transform.Unanchor(ent, xform);

        // shitcode
        _physics.TrySetBodyType(ent, BodyType.KinematicController, fixturesComponent, physicsComponent, xform);
        _physics.SetCollisionLayer(ent, WallFixtureId, fix2, 0);
        _physics.SetCollisionMask(ent, WallFixtureId, fix2, 0);
        ent.Comp.CurrentTotalDamage = FixedPoint2.Zero;

        RemComp<NoRotateOnMoveComponent>(ent);
        RemComp<NoRotateOnInteractComponent>(ent);

        var toDefaultChangedEvent = new Scp999ChangedStateEvent(Scp999States.Default);
        RaiseLocalEvent(ent, toDefaultChangedEvent);

        var ev = new Scp999WallifyEvent(GetNetEntity(ent), ent.Comp.States[Scp999States.Default]);
        RaiseNetworkEvent(ev);
    }
}
