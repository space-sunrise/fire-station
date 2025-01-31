using Content.Server._Scp.Scp106.Components;
using Content.Server.Construction.Completions;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106PortalSpawnerSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entity = default!;


    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<Scp106PortalSpawnerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator < 60)
                return;

            Spawn(comp.Monster, Transform(uid).Coordinates);
            comp.Accumulator -= 60;
            comp.SpawnedMonsters += 1;

            if (comp.SpawnedMonsters < 5)
                return;

            _entity.DeleteEntity(uid);
        }
    }
}
