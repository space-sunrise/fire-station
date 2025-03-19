using System.Linq;
using Content.Server._Scp.Helpers;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.Artifacts.Effects.RandomTransformation;

public sealed class ArtifactRandomTransformationSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactRandomTransformationComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<ArtifactRandomTransformationComponent> ent, ref ArtifactActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var entities = _lookup.GetEntitiesInRange<ItemComponent>(coords, ent.Comp.Radius)
            .Select(e => e.Owner)
            .ToHashSet();

        SearchPlayersInventoryForItems(ent, coords, out var inventoryItems);

        ReduceAndTransform(ent, inventoryItems);
        ReduceAndTransform(ent, entities);
    }

    private void SearchPlayersInventoryForItems(Entity<ArtifactRandomTransformationComponent> ent, EntityCoordinates coords, out HashSet<EntityUid> items)
    {
        var players = _lookup.GetEntitiesInRange<InventoryComponent>(coords, ent.Comp.Radius);
        items = new HashSet<EntityUid>();

        foreach (var player in players)
        {
            var inventorySlots = _inventory.GetSlotEnumerator(player.Owner);

            while (inventorySlots.MoveNext(out var slot))
            {
                if (!_inventory.TryGetSlotEntity(player, slot.ID, out var itemUid))
                    continue;

                items.Add(itemUid.Value);
            }
        }
    }

    private void ReduceAndTransform(Entity<ArtifactRandomTransformationComponent> ent, IReadOnlyCollection<EntityUid> entities)
    {
        var items = _scpHelpers.GetPercentageOfHashSet(entities, ent.Comp.TransformationPercentRatio);

        DoTransformation(ent, items);
    }

    private void DoTransformation(Entity<ArtifactRandomTransformationComponent> ent, IEnumerable<EntityUid> items)
    {
        foreach (var item in items)
        {
            if (!_prototype.TryGetRandom<EntityPrototype>(_random, out var prototype))
                continue;

            var proto = (EntityPrototype) prototype;

            if (!CanSpawnEntity(ent, proto))
                continue;

            /*
             * TODO: Обработка ентити в конейтейнерах
             * Требуется сделать проверку, что если ентити находится в конейнере
             * То после создания нового оно помещается в тот же слот конейтенера
             */

            Spawn(prototype.ID, _transform.GetMapCoordinates(item));
            QueueDel(item);
        }
    }

    private static bool CanSpawnEntity(Entity<ArtifactRandomTransformationComponent> ent, EntityPrototype proto)
    {
        if (ent.Comp.PrototypeBlacklist != null && ent.Comp.PrototypeBlacklist.Contains(proto.ID))
            return false;

        if (proto.Abstract)
            return false;

        if (ent.Comp.CategoryBlacklist != null && proto.Categories.Any(c => ent.Comp.CategoryBlacklist.Contains(c)))
            return false;

        return true;
    }
}
