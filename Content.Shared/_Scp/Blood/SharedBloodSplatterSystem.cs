using System.Diagnostics;
using Content.Shared._Scp.Helpers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blood;

public abstract class SharedBloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PredictedRandomSystem _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodParticleComponent, ComponentInit>(OnInit);

        Log.Level = LogLevel.Info;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Проходимся по всем частичкам крови и двигаем их
        var query = EntityQueryEnumerator<BloodParticleComponent, FixturesComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var particle, out var fixtures, out var physics))
        {
            if (!particle.Velocity.HasValue)
                continue;

            if (_timing.CurTime < particle.NextMoveTime)
                continue;

            _physics.ApplyLinearImpulse(uid, particle.Velocity.Value, fixtures, physics);
            particle.NextMoveTime = _timing.CurTime + particle.MoveCooldown;
        }
    }

    protected virtual void OnInit(Entity<BloodParticleComponent> ent, ref ComponentInit args)
    {
        Debug.Assert(ent.Comp.MoveTimes == 0);
        ent.Comp.FlyTime += ent.Comp.FlyTime * _random.NextFloatForEntity(ent, 0f, ent.Comp.FlyTimeVariation);

        Log.Info($"{ent.Comp.FlyTime}");
    }
}
