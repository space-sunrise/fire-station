using Content.Server._Scp.LightFlicking.Components;
using Content.Server.Light.EntitySystems;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem : EntitySystem
{
    private const float FlickingStartChance = 0.2f;

    private readonly TimeSpan _flickCheckInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _flickCheckVariation = TimeSpan.FromMinutes(1);

    private void OnMapInit(Entity<LightFlickingComponent> ent, ref MapInitEvent args)
    {
        SetNextFlickingStartTime(ent);
    }

    private void OnLightEject(Entity<LightFlickingComponent> ent, ref LightEjectEvent args)
    {
        ent.Comp.Enabled = false;
    }

    private void OnLightInsert(Entity<LightFlickingComponent> ent, ref LightInsertEvent args)
    {
        if (!HasComp<MalfunctionLightComponent>(args.Bulb))
            return;

        ent.Comp.Enabled = true;
    }

    private void SetNextFlickingStartTime(Entity<LightFlickingComponent> ent)
    {
        var variation = _flickCheckInterval - _random.Next(_flickCheckVariation);
        ent.Comp.NextFlickStartChanceTime = _timing.CurTime + variation;
    }

    private void MalfunctionBulb(EntityUid lightUid)
    {
        if (!TryGetBulb(lightUid, out var bulb))
            return;

        EnsureComp<MalfunctionLightComponent>(bulb.Value);
    }
}
