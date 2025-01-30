using System.Diagnostics.CodeAnalysis;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared._Scp.LightFlicking;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.LightFlicking;

public sealed class LightFlickingSystem : EntitySystem
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float FlickingStartChance = 0.2f;
    private readonly TimeSpan _flickCheckInterval = TimeSpan.FromMinutes(20);
    private readonly TimeSpan _flickCheckVariation = TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightFlickingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<LightFlickingComponent, LightEjectEvent>(OnLightEject);
        SubscribeLocalEvent<LightFlickingComponent, LightInsertEvent>(OnLightInsert);
    }

    private void OnMapInit(Entity<LightFlickingComponent> ent, ref MapInitEvent args)
    {
        SetNextTime(ent);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<LightFlickingComponent, PoweredLightComponent>();

        while (query.MoveNext(out var uid, out var flicking, out _))
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
                SetNextTime((uid, flicking));
                continue;
            }

            flicking.Enabled = true;
            MalfunctionBulb(uid);

            Dirty(uid, flicking);
        }

    }

    private void SetNextTime(Entity<LightFlickingComponent> ent)
    {
        var variation = _flickCheckInterval - _random.Next(_flickCheckVariation);
        ent.Comp.NextFlickStartChanceTime = _timing.CurTime + variation;
    }

    private bool TryGetBulb(EntityUid lightUid, [NotNullWhen(true)] out EntityUid? bulb)
    {
        bulb = _poweredLight.GetBulb(lightUid);

        if (!bulb.HasValue)
            return false;

        bulb = bulb.Value;

        return true;
    }

    private void MalfunctionBulb(EntityUid lightUid)
    {
        if (!TryGetBulb(lightUid, out var bulb))
            return;

        EnsureComp<MalfunctionLightComponent>(bulb.Value);
    }
}
