using Content.Server._Scp.Blood;
using Content.Shared._Scp.Blood;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Misc.LiquidParticlesGenerator;

public sealed class LiquidParticlesGeneratorSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BloodSplatterSystem _bloodSplatter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LiquidParticlesGeneratorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LiquidParticlesGeneratorComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.EnableOnMapInit)
            return;

        ent.Comp.Enabled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LiquidParticlesGeneratorComponent, BloodSplattererComponent>();
        while (query.MoveNext(out var uid, out var liquid, out var bloodSplatterer))
        {
            // TODO: Заменить на ActiveComponent логику, если потребуется оптимизация
            if (!liquid.Enabled)
                continue;

            if (_timing.CurTime < liquid.NextSpawn)
                continue;

            SpawnParticle((uid, bloodSplatterer, liquid));

            var variation = _random.Next(liquid.CooldownVariation);
            liquid.NextSpawn = _timing.CurTime + liquid.Cooldown + variation;
        }
    }

    private void SpawnParticle(Entity<BloodSplattererComponent, LiquidParticlesGeneratorComponent> target)
    {
        // TODO: Тут чето сделать и починить
        var worldRot = Transform(target).LocalRotation;
        var angle = (float) worldRot.Degrees + target.Comp2.Angle;

        _bloodSplatter.SpawnBloodParticles(target, target, Angle.FromDegrees(target.Comp2.Angle), target.Comp2.Radians);
    }
}
