using System.Diagnostics.CodeAnalysis;
using Content.Server._Scp.LightFlicking.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared._Scp.LightFlicking;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem : SharedLightFlickingSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightFlickingComponent, MapInitEvent>(OnMapInit, after: [typeof(PoweredLightSystem)]);

        SubscribeLocalEvent<LightFlickingComponent, LightEjectEvent>(OnLightEject);
        SubscribeLocalEvent<LightFlickingComponent, LightInsertEvent>(OnLightInsert);
    }

    private void OnMapInit(Entity<LightFlickingComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<PoweredLightComponent>(ent))
            return;

        SetupFlicking(ent);
        SetNextFlickingStartTime(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<LightFlickingComponent, PoweredLightComponent>();

        // Обработка "поврждения" лампочек для пометки их как мигающие
        while (query.MoveNext(out var uid, out var flicking, out _))
        {
            if (Timing.CurTime <= flicking.NextFlickStartChanceTime)
                continue;

            if (!TryGetBulb(uid, out var bulb))
                continue;

            if (HasComp<MalfunctionLightComponent>(bulb))
                continue;

            if (flicking.Enabled)
                continue;

            if (!Random.Prob(FlickingStartChance))
            {
                SetNextFlickingStartTime((uid, flicking));
                continue;
            }

            flicking.Enabled = true;
            Dirty(uid, flicking);

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

    private bool TryGetBulbEntity(EntityUid lightUid, [NotNullWhen(true)] out Entity<LightBulbComponent>? bulb)
    {
        bulb = null;
        var bulbUid = _poweredLight.GetBulb(lightUid);

        if (!bulbUid.HasValue)
            return false;

        if (!TryComp<LightBulbComponent>(bulbUid, out var lightBulbComponent))
            return false;

        bulb = (bulbUid.Value, lightBulbComponent);

        return true;
    }
}
