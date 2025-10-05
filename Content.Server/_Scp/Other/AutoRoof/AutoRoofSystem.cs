using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._Scp.Other.AutoRoof;

public sealed class AutoRoofSystem : EntitySystem
{
    [Dependency] private readonly RoofSystem _roof = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoRoofComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AutoRoofComponent> ent, ref ComponentInit args)
    {
        var grid = Transform(ent).GridUid;
        if (!grid.HasValue)
            return;

        BuildRoof(grid.Value);
        QueueDel(ent);
    }

    private void BuildRoof(Entity<MapGridComponent?> grid)
    {
        if (!Resolve(grid, ref grid.Comp))
            return;

        RemComp<ImplicitRoofComponent>(grid);
        EnsureComp<RoofComponent>(grid);

        foreach (var tile in _map.GetAllTiles(grid, grid.Comp))
        {
            _roof.SetRoof(grid, tile.GridIndices, true, true);
        }
    }
}
