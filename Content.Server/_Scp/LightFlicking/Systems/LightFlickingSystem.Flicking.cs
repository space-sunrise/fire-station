using Content.Shared._Scp.LightFlicking;

namespace Content.Server._Scp.LightFlicking.Systems;

public sealed partial class LightFlickingSystem
{
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
}
