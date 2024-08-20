using System.Linq;
using System.Threading;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Scp2398;

public sealed class Scp2398System : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<Scp2398Component, MeleeHitEvent>(OnMeleeHitEvent);
        SubscribeLocalEvent<Scp2398Component, ThrowDoHitEvent>(OnThrowHitEvent);
    }

    private void OnMeleeHitEvent(EntityUid uid, Scp2398Component component, MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities)
        {
            TryExplode(uid, hitEntity);
        }
    }

    private void OnThrowHitEvent(EntityUid uid, Scp2398Component component, ThrowDoHitEvent args)
    {
        // По какой-то странной причине, оно не выдает что я ожидаю от него
        // if (_physics.GetMapLinearVelocity(uid).Length() <= component.TriggerThrowSpeed)
        //     return;
        TryExplode(uid, args.Target);
    }

    private void TryExplode(EntityUid uid, EntityUid target)
    {
        // В оригинальной статье не указано, работает ли бита на мертвых. Так что она у нас не работает.
        if (!HasComp<MobStateComponent>(target) || _mob.IsDead(target) || _mob.IsCritical(target))
            return;

        Explode(target);
    }

    private void Explode(EntityUid target)
    {
        if (!TryComp<PhysicsComponent>(target, out var physics))
            return;

        var coords = _transform.GetMapCoordinates(target);
        Timer.Spawn(_timing.TickPeriod,
            () => _explosion.QueueExplosion(coords,
                ExplosionSystem.DefaultExplosionPrototypeId,
            physics.Mass * 2,
            10,
                10000,
            target,
            maxTileBreak: 0),
            CancellationToken.None);

        // _mob.ChangeMobState(target, MobState.Dead);  // Оно должно гарантированно убивать цель

        // Если надо будет - можно и гибать :)
        // _body.GibBody(target);
    }
}
