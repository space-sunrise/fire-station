using Content.Server.Disposal.Unit;
using Content.Server.Popups;
using Content.Shared._Scp.Scp999;
using Content.Shared._Sunrise.VentCraw;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp999;

public sealed class Scp999System : SharedScp999System
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string WallFixtureId = "fix2";

    private readonly SoundSpecifier _wallSound = new SoundCollectionSpecifier("WallTransformScp999");
    private readonly SoundSpecifier _sleepSound = new SoundPathSpecifier("/Audio/_Scp/Scp999/sleep.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp999Component, Scp999WallifyActionEvent>(OnWallifyActionEvent);
        SubscribeLocalEvent<Scp999Component, Scp999RestActionEvent>(OnRestActionEvent);
        SubscribeLocalEvent<Scp999Component, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<Scp999Component, Scp999ChangeStateAttemptEvent>(OnChangeStateAttempt);
        SubscribeLocalEvent<Scp999Component, Scp999ChangedStateEvent>(OnChangedState);

        SubscribeLocalEvent<Scp999Component, EntityFedEvent>(OnFeed);
    }

    #region Abilities

    private void OnMobStateChanged(Entity<Scp999Component> entity, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        entity.Comp.CurrentState = Scp999States.Default;
        Dirty(entity);
    }

    private void OnWallifyActionEvent(Entity<Scp999Component> ent, ref Scp999WallifyActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixturesComponent))
            return;

        var xform = Transform(ent);

        var fix2 = _fixture.GetFixtureOrNull(ent, WallFixtureId, fixturesComponent);

        if (fix2 == null)
            return;

        Scp999WallifyEvent ev;
        var netEntity = GetNetEntity(ent);

        switch (ent.Comp.CurrentState)
        {
            // add buffs
            case Scp999States.Default:

                var toWallAttemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Wall);
                RaiseLocalEvent(ent, toWallAttemptEvent);

                if (toWallAttemptEvent.Cancelled)
                    return;

                ev = new Scp999WallifyEvent(netEntity, ent.Comp.States[Scp999States.Wall]);

                ent.Comp.CurrentState = Scp999States.Wall;
                Dirty(ent);

                _transform.AnchorEntity(ent, Transform(ent));

                // shitcode
                _physics.TrySetBodyType(ent, BodyType.Dynamic, fixturesComponent, physicsComponent, xform);
                _physics.SetCollisionLayer(ent, WallFixtureId, fix2, 221);
                _physics.SetCollisionMask(ent, WallFixtureId, fix2, 158);

                EnsureComp<NoRotateOnInteractComponent>(ent);
                EnsureComp<NoRotateOnMoveComponent>(ent);

                _audio.PlayPvs(_wallSound, ent);

                var toWallChangedEvent = new Scp999ChangedStateEvent(Scp999States.Wall);
                RaiseLocalEvent(ent, toWallChangedEvent);

                break;

            // remove buffs
            case Scp999States.Wall:

                var toDefaultAttemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Default);
                RaiseLocalEvent(ent, toDefaultAttemptEvent);

                if (toDefaultAttemptEvent.Cancelled)
                    return;

                ev = new Scp999WallifyEvent(netEntity, ent.Comp.States[Scp999States.Default]);

                ent.Comp.CurrentState = Scp999States.Default;
                Dirty(ent);

                _transform.Unanchor(ent, Transform(ent));

                // shitcode
                _physics.TrySetBodyType(ent, BodyType.KinematicController, fixturesComponent, physicsComponent, xform);
                _physics.SetCollisionLayer(ent, WallFixtureId, fix2, 0);
                _physics.SetCollisionMask(ent, WallFixtureId, fix2, 0);

                RemComp<NoRotateOnMoveComponent>(ent);
                RemComp<NoRotateOnInteractComponent>(ent);

                var toDefaultChangedEvent = new Scp999ChangedStateEvent(Scp999States.Default);
                RaiseLocalEvent(ent, toDefaultChangedEvent);

                break;

            // Все остальное
            default:
                return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }

    private void OnRestActionEvent(Entity<Scp999Component> ent, ref Scp999RestActionEvent args)
    {
        if (args.Handled)
            return;

        Scp999RestEvent ev;
        var netEntity = GetNetEntity(ent);

        switch (ent.Comp.CurrentState)
        {
            // add buffs
            // TODO: РЕАЛЬНЫЙ сон, а не вот это параша
            case Scp999States.Default:

                var toRestAttemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Rest);
                RaiseLocalEvent(ent, toRestAttemptEvent);

                if (toRestAttemptEvent.Cancelled)
                    return;

                ev = new Scp999RestEvent(netEntity, ent.Comp.States[Scp999States.Rest]);

                ent.Comp.CurrentState = Scp999States.Rest;
                Dirty(ent);

                EnsureComp<BlockMovementComponent>(ent);
                EnsureComp<NoRotateOnInteractComponent>(ent);
                EnsureComp<NoRotateOnMoveComponent>(ent);

                _audio.PlayPvs(_sleepSound, ent);

                var toRestChangedEvent = new Scp999ChangedStateEvent(Scp999States.Rest);
                RaiseLocalEvent(ent, toRestChangedEvent);

                break;

            // remove buffs
            case Scp999States.Rest:

                var toDefaultAttemptEvent = new Scp999ChangeStateAttemptEvent(Scp999States.Default);
                RaiseLocalEvent(ent, toDefaultAttemptEvent);

                if (toDefaultAttemptEvent.Cancelled)
                    return;

                ev = new Scp999RestEvent(netEntity, ent.Comp.States[Scp999States.Default]);

                ent.Comp.CurrentState = Scp999States.Default;
                Dirty(ent);

                RemComp<NoRotateOnMoveComponent>(ent);
                RemComp<NoRotateOnInteractComponent>(ent);
                RemComp<BlockMovementComponent>(ent);

                var toDefaultChangedEvent = new Scp999ChangedStateEvent(Scp999States.Default);
                RaiseLocalEvent(ent, toDefaultChangedEvent);

                break;

            // Все остальное
            default:
                return;
        }

        RaiseNetworkEvent(ev);

        args.Handled = true;
    }

    #endregion

    private void OnChangeStateAttempt(Entity<Scp999Component> ent, ref Scp999ChangeStateAttemptEvent args)
    {
        if (_container.IsEntityInContainer(ent))
            args.Cancel();

        if (HasComp<BeingDisposedComponent>(ent))
            args.Cancel();

        if (TryComp<VentCrawlerComponent>(ent, out var ventCrawler) && ventCrawler.InTube)
            args.Cancel();

        if (args.Cancelled)
            _popup.PopupEntity(Loc.GetString("scp-999-change-state-cancelled"), ent, ent);
    }

    private void OnChangedState(Entity<Scp999Component> ent, ref Scp999ChangedStateEvent _)
    {
        // Чтобы в момент превращения прекращать тащить и быть таскаемым.

        if (TryComp<PullableComponent>(ent, out var pullable))
            _pulling.TryStopPull(ent, pullable);

        if (TryComp<PullerComponent>(ent, out var puller) && puller.Pulling.HasValue && TryComp<PullableComponent>(puller.Pulling, out var pullable2))
            _pulling.TryStopPull(puller.Pulling.Value, pullable2, ent);
    }

    private void OnFeed(Entity<Scp999Component> scp, ref EntityFedEvent args)
    {
        if (!_tag.HasTag(args.Food, scp.Comp.CandyTag))
            return;

        if (!_random.Prob(scp.Comp.CreateJellyChance))
            return;

        Spawn(scp.Comp.Scp999Jelly, Transform(scp).Coordinates);

        if (scp.Comp.CreateJellySound != null)
            _audio.PlayPvs(scp.Comp.CreateJellySound, scp);
    }
}
