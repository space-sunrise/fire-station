using Content.Server._Scp.LightFlicking.Components;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem
{
    private const float RadiusVariationPercentage = 0.2f;
    private const float EnergyVariationPercentage = 0.2f;

    private readonly TimeSpan _flickInterval = TimeSpan.FromSeconds(0.5);
    private readonly TimeSpan _flickVariation = TimeSpan.FromSeconds(0.45);

    private void OnInit(Entity<LightFlickingComponent> ent, ref ComponentInit args)
    {
        var light = _pointLight.EnsureLight(ent);
        ent.Comp.DumpedRadius = light.Radius;
        ent.Comp.DumpedEnergy = light.Energy;

        SetNextFlickingTime(ent);
    }

    private void SetNextFlickingTime(Entity<LightFlickingComponent> ent)
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
