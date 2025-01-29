using Content.Shared._Scp.LightFlicking;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Scp.LightFlicking;

// TODO: При отключении в настройках возвращать энергию и радиус в стандартное состояние
// TODO: Пофиксить, что при неком стечении обстоятельств лампочка может 999 раз уменьшить энергию или радиус, пока не выключится и наоборот

public sealed class LightFlickingSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _enabled;

    private const float RadiusVariationPercentage = 0.07f;
    private const float EnergyVariationPercentage = 0.03f;

    private readonly TimeSpan _flickInterval = TimeSpan.FromSeconds(0.5);
    private readonly TimeSpan _flickVariation = TimeSpan.FromSeconds(0.35);

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.LightFlickingEnable, enabled => _enabled = enabled, true);

        SubscribeLocalEvent<LightFlickingComponent, ComponentInit>(OnInit);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.LightFlickingEnable, enabled => _enabled = enabled);
    }

    private void OnInit(Entity<LightFlickingComponent> ent, ref ComponentInit args)
    {
        SetNextTime(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        var query = AllEntityQuery<LightFlickingComponent>();

        while (query.MoveNext(out var uid, out var flicking))
        {
            if (_timing.CurTime <= flicking.NextFlickTime)
                continue;

            var light = _pointLight.EnsureLight(uid);
            _pointLight.SetEnergy(uid, Variantize(light.Energy, EnergyVariationPercentage));
            _pointLight.SetRadius(uid, Variantize(light.Radius, RadiusVariationPercentage));

            SetNextTime((uid, flicking));
        }
    }

    private void SetNextTime(Entity<LightFlickingComponent> ent)
    {
        var additionalTime = _flickInterval - _random.Next(_flickVariation);
        ent.Comp.NextFlickTime = _timing.CurTime + additionalTime;
    }

    private float Variantize(float origin, float baseVariation)
    {
        var variation = (float)(_random.NextDouble() * (2 * baseVariation) - baseVariation);
        return origin * (1 + variation);
    }
}
