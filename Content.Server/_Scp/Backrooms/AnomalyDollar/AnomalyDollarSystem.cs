using Content.Server.Explosion.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Explosion.Components;
using Content.Shared.Atmos;

namespace Content.Server._Scp.Backrooms.AnomalyDollar;

public sealed partial class AnomalyDollarSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyDollarComponent, IgnitedEvent>(OnIgnited);
    }

    private void OnIgnited(Entity<AnomalyDollarComponent> ent, ref IgnitedEvent args)
    {
        if (Count<AnomalyDollarComponent>() > 1 ||
            !TryComp<ExplosiveComponent>(ent, out var explosionComp))
            return;

        _explosion.QueueExplosion(
            _transform.GetMapCoordinates(ent),
            explosionComp.ExplosionType,
            totalIntensity: explosionComp.TotalIntensity,
            slope: explosionComp.IntensitySlope,
            maxTileIntensity: explosionComp.MaxIntensity,
            cause: ent.Owner,
            addLog: true
        );
    }
}
