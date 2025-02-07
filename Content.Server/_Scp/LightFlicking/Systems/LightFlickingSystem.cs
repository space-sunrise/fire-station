using System.Diagnostics.CodeAnalysis;
using Content.Server._Scp.LightFlicking.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightFlickingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LightFlickingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<LightFlickingComponent, LightEjectEvent>(OnLightEject);
        SubscribeLocalEvent<LightFlickingComponent, LightInsertEvent>(OnLightInsert);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var flickingQuery = AllEntityQuery<LightFlickingComponent>();

        // Обработка мигания лампочек
        while (flickingQuery.MoveNext(out var uid, out var flicking))
        {
            if (!flicking.Enabled)
                continue;

            if (_timing.CurTime <= flicking.NextFlickTime)
                continue;

            _pointLight.SetEnergy(uid, Variantize(flicking.DumpedEnergy, EnergyVariationPercentage));
            _pointLight.SetRadius(uid, Variantize(flicking.DumpedRadius, RadiusVariationPercentage));

            SetNextFlickingTime((uid, flicking));
        }

        var normalQuery = AllEntityQuery<LightFlickingComponent, PoweredLightComponent>();

        // Обработка "поврждения" лампочек для пометки их как мигающие
        while (normalQuery.MoveNext(out var uid, out var flicking, out _))
        {
            if (_timing.CurTime <= flicking.NextFlickStartChanceTime)
                continue;

            if (!TryGetBulb(uid, out var bulb))
                continue;

            if (HasComp<MalfunctionLightComponent>(bulb))
                continue;

            if (flicking.Enabled)
                continue;

            if (!_random.Prob(FlickingStartChance))
            {
                SetNextFlickingStartTime((uid, flicking));
                continue;
            }

            flicking.Enabled = true;
            MalfunctionBulb(uid);
        }
    }

    private bool TryGetBulb(EntityUid lightUid, [NotNullWhen(true)] out EntityUid? bulb)
    {
        bulb = _poweredLight.GetBulb(lightUid);

        if (!bulb.HasValue)
            return false;

        bulb = bulb.Value;

        return true;
    }
}
