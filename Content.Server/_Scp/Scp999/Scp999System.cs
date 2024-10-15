using Content.Shared._Scp.Scp999;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Scp.Scp999;

public sealed class Scp999System : SharedScp999System
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

    private const string WallFixtureId = "fix2";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp999Component, Scp999WallifyActionEvent>(OnWallifyActionEvent);
        SubscribeLocalEvent<Scp999Component, Scp999RestActionEvent>(OnRestActionEvent);
        SubscribeLocalEvent<Scp999Component, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<Scp999Component> entity, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        entity.Comp.CurrentState = Scp999States.Default;
        Dirty(entity);
    }

    private void OnWallifyActionEvent(Entity<Scp999Component> entity, ref Scp999WallifyActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PhysicsComponent>(entity, out var physicsComponent))
            return;

        if (!TryComp<FixturesComponent>(entity, out var fixturesComponent))
            return;

        var xform = Transform(entity);

        var fix2 = _fixture.GetFixtureOrNull(entity, WallFixtureId, fixturesComponent);

        if (fix2 == null)
            return;

        Scp999WallifyEvent ev;
        var netEntity = GetNetEntity(entity);

        switch (entity.Comp.CurrentState)
        {
            // add buffs
            case Scp999States.Default:
                ev = new Scp999WallifyEvent(netEntity, entity.Comp.States[Scp999States.Wall]);

                entity.Comp.CurrentState = Scp999States.Wall;
                Dirty(entity);

                _transform.AnchorEntity(entity, Transform(entity));

                // shitcode
                _physics.TrySetBodyType(entity, BodyType.Dynamic, fixturesComponent, physicsComponent, xform);
                _physics.SetCollisionLayer(entity, WallFixtureId, fix2, 221);
                _physics.SetCollisionMask(entity, WallFixtureId, fix2, 158);

                EnsureComp<NoRotateOnInteractComponent>(entity);
                EnsureComp<NoRotateOnMoveComponent>(entity);

                RemComp<PullableComponent>(entity);

                break;

            // remove buffs
            case Scp999States.Wall:
                ev = new Scp999WallifyEvent(netEntity, entity.Comp.States[Scp999States.Default]);

                entity.Comp.CurrentState = Scp999States.Default;
                Dirty(entity);

                _transform.Unanchor(entity, Transform(entity));

                // shitcode
                _physics.TrySetBodyType(entity, BodyType.KinematicController, fixturesComponent, physicsComponent, xform);
                _physics.SetCollisionLayer(entity, WallFixtureId, fix2, 0);
                _physics.SetCollisionMask(entity, WallFixtureId, fix2, 0);

                RemComp<NoRotateOnMoveComponent>(entity);
                RemComp<NoRotateOnInteractComponent>(entity);

                EnsureComp<PullableComponent>(entity);

                break;

            // Все остальное
            default:
                return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }

    private void OnRestActionEvent(Entity<Scp999Component> entity, ref Scp999RestActionEvent args)
    {
        if (args.Handled)
            return;

        Scp999RestEvent ev;
        var netEntity = GetNetEntity(entity);

        switch (entity.Comp.CurrentState)
        {
            // add buffs
            case Scp999States.Default:
                ev = new Scp999RestEvent(netEntity, entity.Comp.States[Scp999States.Rest]);

                entity.Comp.CurrentState = Scp999States.Rest;
                Dirty(entity);

                EnsureComp<BlockMovementComponent>(entity);
                EnsureComp<NoRotateOnInteractComponent>(entity);
                EnsureComp<NoRotateOnMoveComponent>(entity);

                break;

            // remove buffs
            case Scp999States.Rest:
                ev = new Scp999RestEvent(netEntity, entity.Comp.States[Scp999States.Default]);

                entity.Comp.CurrentState = Scp999States.Default;
                Dirty(entity);

                RemComp<NoRotateOnMoveComponent>(entity);
                RemComp<NoRotateOnInteractComponent>(entity);
                RemComp<BlockMovementComponent>(entity);

                break;

            // Все остальное
            default:
                return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }
}
