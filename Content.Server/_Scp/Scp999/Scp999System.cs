using Content.Shared._Scp.Scp999;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Scp.Scp999;

public sealed class Scp999System : SharedScp999System
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("scp.999");
        SubscribeLocalEvent<Scp999Component, Scp999WallifyActionEvent>(OnWallifyActionEvent);
        SubscribeLocalEvent<Scp999Component, Scp999RestActionEvent>(OnRestActionEvent);
        SubscribeLocalEvent<Scp999Component, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, Scp999Component component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        component.CurrentState = Scp999States.Default;
    }

    private void OnWallifyActionEvent(EntityUid uid, Scp999Component component, ref Scp999WallifyActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PhysicsComponent>(uid, out var physicsComponent))
            return;

        if (!TryComp<FixturesComponent>(uid, out var fixturesComponent))
            return;

        var xform = Transform(uid);

        var fix2 = _fixture.GetFixtureOrNull(uid, "fix2", fixturesComponent);
        if (fix2 == null)
            return;

        Scp999WallifyEvent ev;

        if (component.CurrentState == Scp999States.Default)
        {
            // add buffs
            ev = new Scp999WallifyEvent(GetNetEntity(uid), component.States[Scp999States.Wall]);
            component.CurrentState = Scp999States.Wall;
            _transform.AnchorEntity(uid, Transform(uid));
            // shitcode
            _physics.TrySetBodyType(uid, BodyType.Dynamic, fixturesComponent, physicsComponent, xform);
            _physics.SetCollisionLayer(uid, "fix2", fix2, 221);
            _physics.SetCollisionMask(uid, "fix2", fix2, 158);
            EnsureComp<NoRotateOnInteractComponent>(uid);
            EnsureComp<NoRotateOnMoveComponent>(uid);
        }
        else if (component.CurrentState == Scp999States.Wall)
        {
            // remove buffs
            ev = new Scp999WallifyEvent(GetNetEntity(uid), component.States[Scp999States.Default]);
            component.CurrentState = Scp999States.Default;
            _transform.Unanchor(uid, Transform(uid));
            // shitcode
            _physics.TrySetBodyType(uid, BodyType.KinematicController, fixturesComponent, physicsComponent, xform);
            _physics.SetCollisionLayer(uid, "fix2", fix2, 0);
            _physics.SetCollisionMask(uid, "fix2", fix2, 0);
            if (HasComp<NoRotateOnMoveComponent>(uid))
                RemComp<NoRotateOnMoveComponent>(uid);
            if (HasComp<NoRotateOnInteractComponent>(uid))
                RemComp<NoRotateOnInteractComponent>(uid);
        }
        else
        {
            return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }

    private void OnRestActionEvent(EntityUid uid, Scp999Component component, ref Scp999RestActionEvent args)
    {
        _sawmill.Debug($"onrestaction {component.CurrentState}");

        if (args.Handled)
            return;

        Scp999RestEvent ev;

        if (component.CurrentState == Scp999States.Default)
        {
            // add buffs
            ev = new Scp999RestEvent(GetNetEntity(uid), component.States[Scp999States.Rest]);
            component.CurrentState = Scp999States.Rest;
            EnsureComp<BlockMovementComponent>(uid);
            // EnsureComp<SleepingComponent>(uid);
        }
        else if (component.CurrentState == Scp999States.Rest)
        {
            _sawmill.Debug($"onrestaction remove debuffs {component.CurrentState}");
            // remove buffs
            ev = new Scp999RestEvent(GetNetEntity(uid), component.States[Scp999States.Default]);
            component.CurrentState = Scp999States.Default;
            if (HasComp<BlockMovementComponent>(uid))
                RemComp<BlockMovementComponent>(uid);
            // if (HasComp<SleepingComponent>(uid))
            //     RemComp<SleepingComponent>(uid);
        }
        else
        {
            return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }
}
