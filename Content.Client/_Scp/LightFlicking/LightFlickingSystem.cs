using Content.Shared._Scp.LightFlicking;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Scp.LightFlicking;

public sealed class LightFlickingSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _enabled;

    private const float RadiusVariationPercentage = 0.2f;
    private const float EnergyVariationPercentage = 0.2f;

    private readonly TimeSpan _flickInterval = TimeSpan.FromSeconds(0.5);
    private readonly TimeSpan _flickVariation = TimeSpan.FromSeconds(0.45);

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.LightFlickingEnable, OnSettingsToggle, true);

        SubscribeLocalEvent<LightFlickingComponent, ComponentInit>(OnInit);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.LightFlickingEnable, OnSettingsToggle);
    }

    private void OnInit(Entity<LightFlickingComponent> ent, ref ComponentInit args)
    {
        var light = _pointLight.EnsureLight(ent);
        ent.Comp.DumpedRadius = light.Radius;
        ent.Comp.DumpedEnergy = light.Energy;

        SetNextTime(ent);
    }

    private void OnSettingsToggle(bool enabled)
    {
        _enabled = enabled;

        var query = AllEntityQuery<LightFlickingComponent>();

        if (enabled)
            return;

        // Возвращаем значения на исходные
        while (query.MoveNext(out var uid, out var flicking))
        {
            _pointLight.SetEnergy(uid, flicking.DumpedEnergy);
            _pointLight.SetRadius(uid, flicking.DumpedRadius);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        var query = AllEntityQuery<LightFlickingComponent>();

        while (query.MoveNext(out var uid, out var flicking))
        {
            if (!flicking.Enabled)
                continue;

            if (_timing.CurTime <= flicking.NextFlickTime)
                continue;

            _pointLight.SetEnergy(uid, Variantize(flicking.DumpedEnergy, EnergyVariationPercentage));
            _pointLight.SetRadius(uid, Variantize(flicking.DumpedRadius, RadiusVariationPercentage));

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
