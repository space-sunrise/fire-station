using System.Diagnostics.CodeAnalysis;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared._Scp.LightFlicking;
using Content.Shared._Scp.LightFlicking.MalfunctionLight;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.LightFlicking;

public sealed class LightFlickingSystem : SharedLightFlickingSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly LightBulbSystem _bulb = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    private const float FlickingStartChance = 0.1f;

    private readonly TimeSpan _flickCheckInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _flickCheckVariation = TimeSpan.FromMinutes(15);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightFlickingComponent, MapInitEvent>(OnMapInit, after: [typeof(PoweredLightSystem)]);

        SubscribeLocalEvent<LightFlickingComponent, LightEjectEvent>(OnLightEject);
        SubscribeLocalEvent<LightFlickingComponent, LightInsertEvent>(OnLightInsert);
    }

    #region Event handlers

    private void OnMapInit(Entity<LightFlickingComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<PoweredLightComponent>(ent))
            return;

        SetupFlicking(ent);
        SetNextFlickingStartTime(ent);
    }

    private void OnLightEject(Entity<LightFlickingComponent> ent, ref LightEjectEvent args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
    }

    private void OnLightInsert(Entity<LightFlickingComponent> ent, ref LightInsertEvent args)
    {
        if (!HasComp<MalfunctionLightComponent>(args.Bulb))
            return;

        ent.Comp.Enabled = true;
        Dirty(ent);
    }

    #endregion

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

        if (!TryGetBulb(lightUid, out var bulbUid))
            return false;

        if (!TryComp<LightBulbComponent>(bulbUid, out var lightBulbComponent))
            return false;

        bulb = (bulbUid.Value, lightBulbComponent);

        return true;
    }

    #region Flicking

    private void SetupFlicking(Entity<LightFlickingComponent> ent)
    {
        var light = _pointLight.EnsureLight(ent);
        ent.Comp.DumpedRadius = light.Radius;
        ent.Comp.DumpedEnergy = light.Energy;

        if (TryGetBulbEntity(ent, out var bulb))
            ent.Comp.DumpedColor = bulb.Value.Comp.Color;

        Dirty(ent);

        SetNextFlickingTime(ent);
    }

    #endregion

    #region Flicking start time

    private void SetNextFlickingStartTime(Entity<LightFlickingComponent> ent)
    {
        var variation = _flickCheckInterval - Random.Next(_flickCheckVariation);
        ent.Comp.NextFlickStartChanceTime = Timing.CurTime + variation;

        Dirty(ent);
    }

    private void MalfunctionBulb(EntityUid lightUid)
    {
        if (!TryGetBulb(lightUid, out var bulb))
            return;

        // TODO: Пофиксить, что после вставления лампы она принимает этот коричневый цвет, вместо нужного оригинального
        _bulb.SetColor(bulb.Value, Color.FromHex("#997e65")); // коричневый
        EnsureComp<MalfunctionLightComponent>(bulb.Value);
    }

    #endregion

}
