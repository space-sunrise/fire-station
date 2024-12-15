using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!CanProduce(source))
        {
            return;
        }

        foreach (var (pointPrototype, value) in source.Comp.PointsPerSecond)
        {
            args.Points.TryGetValue(pointPrototype, out var totalValue);
            args.Points[pointPrototype] = totalValue + value;
        }
    }

    public bool CanProduce(Entity<ResearchPointSourceComponent> source)
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }
}
