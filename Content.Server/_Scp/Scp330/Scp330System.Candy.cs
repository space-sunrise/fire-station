using System.Linq;
using Content.Shared._Scp.Other.Events;
using Content.Shared._Scp.Scp330;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp330;

public sealed partial class Scp330System
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<ReagentPrototype> FallbackReagent = "Scp330Nothing";

    private void InitializeCandy()
    {
        SubscribeLocalEvent<Scp330CandyComponent, EntityInsertedIntoStorageEvent>(OnInsertedIntoStorage);
    }

    #region Events

    private void OnInsertedIntoStorage(Entity<Scp330CandyComponent> ent, ref EntityInsertedIntoStorageEvent args)
    {
        if (!TryComp<Scp330BowlComponent>(args.Storage, out var storage))
            return;

        TryDecreaseThiefCounter((args.Storage, storage), ent.Owner);
    }

    #endregion

    #region Assign effects to candies

    private bool TryAssignEffects(Entity<Scp330BowlComponent> ent)
    {
        var container = _container.EnsureContainer<Container>(ent, StorageComponent.ContainerId);
        if (container.Count == 0)
        {
            Log.Error($"Failed to assign effects to SCP-330 candies due to bowl contents {ToPrettyString(ent)} is empty");
            return false;
        }

        foreach (var candy in container.ContainedEntities)
        {
            AssignEffect(ent, candy);
        }

        return true;
    }

    private void AssignEffect(Entity<Scp330BowlComponent> ent, Entity<Scp330CandyComponent?> candy)
    {
        if (!Resolve(candy, ref candy.Comp, false))
            return;

        var proto = Prototype(candy);
        if (proto == null)
            return;

        if (!ent.Comp.CandyEffects.TryGetValue(proto, out var reagent))
            reagent = GetRandomEffect(ent);

        if (!_solution.EnsureSolution(candy.Owner, candy.Comp.SolutionName, out _, out var solution))
            return;

        solution.RemoveAllSolution();
        solution.AddReagent(reagent, candy.Comp.ReagentQuantity);
        ent.Comp.CandyEffects[proto] = reagent;
    }

    #endregion

    #region Helpers

    private ProtoId<ReagentPrototype> GetRandomEffect(Entity<Scp330BowlComponent> ent)
    {
        if (ent.Comp.AvailableReagents.Count == 0)
        {
            Log.Error($"{nameof(Scp330BowlComponent.AvailableReagents)} is empty! This should not be");
            return FallbackReagent;
        }

        var usedReagents = ent.Comp.CandyEffects.Values.ToHashSet();
        var availableUnused = ent.Comp.AvailableReagents
            .Where(r => !usedReagents.Contains(r))
            .ToList();

        if (availableUnused.Count == 0)
        {
            Log.Warning($"All unique reagents for {ent} have been exhausted. Picking a duplicate effect.");
            return _random.Pick(ent.Comp.AvailableReagents);
        }

        return _random.Pick(availableUnused);
    }

    #endregion
}
