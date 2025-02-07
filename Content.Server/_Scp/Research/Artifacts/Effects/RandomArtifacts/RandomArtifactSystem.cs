using System.Linq;
using Content.Server._Scp.Helpers;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Item;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.RandomArtifacts;

public sealed partial class RandomArtifactsSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactsSystem = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float ItemToArtifactRatio = 0.2f; // from 0 to 100. In % percents. Default is 0.2%

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreateRandomArtifactsComponent, ArtifactActivatedEvent>(OnActivate);

        _console.RegisterCommand("listartifacts", "Показывает список всех артефактов-предметов, созданных в результате эффекта от изучения SCP", "listartifacts", ListArtifacts);
    }

    private void OnActivate(Entity<CreateRandomArtifactsComponent> ent, ref ArtifactActivatedEvent args)
    {
        var items = EntityQuery<ItemComponent>().ToList();
        _random.Shuffle(items);

        var selectedItems = _scpHelpers.GetPercentageOfHashSet(items, ItemToArtifactRatio);

        foreach (var item in selectedItems)
        {
            var entity = item.Owner;

            var artifactComponent = EnsureComp<ArtifactComponent>(entity);
            _artifactsSystem.RandomizeArtifact(entity, artifactComponent);
        }
    }
}
