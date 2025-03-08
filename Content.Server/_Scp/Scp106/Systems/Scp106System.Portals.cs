using Content.Server._Scp.Scp106.Components;
using Content.Shared._RMC14.Xenonids.Screech;
using Content.Shared.Coordinates;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp106.Systems;

public sealed partial class Scp106System
{
    private void OnPortalSpawn(Entity<Scp106PortalSpawnerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextSpawnTime = _timing.CurTime;
    }

    private void CheckPortals()
    {
        var query = AllEntityQuery<Scp106PortalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextSpawnTime > _timing.CurTime)
                continue;

            SpawnCreature(comp.Monster, (uid, comp));

            if (comp.MonsterAccumulator < comp.MonsterAccumulatorBound)
                continue;

            SpawnCreature(comp.BigMonster, (uid, comp));

            comp.MonsterAccumulator = 0;
        }
    }

    private void SpawnCreature(EntProtoId ent, Entity<Scp106PortalSpawnerComponent> portal)
    {
        Spawn(ent, Transform(portal).Coordinates);

        portal.Comp.MonsterAccumulator += 1;
        portal.Comp.NextSpawnTime = _timing.CurTime + PortalSpawnRate;

        StartScreech(portal);
    }

    private void StartScreech(EntityUid uid, XenoScreechComponent? component = null, bool playSound = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (playSound)
            _audio.PlayPvs(component.Sound, uid);

        SpawnAttachedTo(component.Effect, uid.ToCoordinates());
    }
}
