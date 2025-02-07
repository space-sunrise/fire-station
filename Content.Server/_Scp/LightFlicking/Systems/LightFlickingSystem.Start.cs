using Content.Server._Scp.LightFlicking.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared._Scp.LightFlicking;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem
{
    private const float FlickingStartChance = 0.1f;

    private readonly TimeSpan _flickCheckInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _flickCheckVariation = TimeSpan.FromMinutes(15);

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

        EnsureComp<MalfunctionLightComponent>(bulb.Value);
    }
}
