using Content.Shared._Scp.Scp330;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp330;

public sealed partial class Scp330System
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<ReagentPrototype> FallbackReagent = "Scp330Nothing";
    private const int MaximumGetUniqueReagentTries = 10;

    private bool TryAssignEffects(Entity<Scp330BowlComponent> ent)
    {
        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        if (container.Count == 0)
        {
            Log.Error($"Failed to assign effects to SCP-330 candies due to bowl contents {ToPrettyString(ent)} is empty. Probably this is race condition issue.");
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

    private ProtoId<ReagentPrototype> GetRandomEffect(Entity<Scp330BowlComponent> ent)
    {
        var tries = 0;
        ProtoId<ReagentPrototype> reagent;

        do
        {
            tries++;
            if (tries > MaximumGetUniqueReagentTries)
            {
                Log.Error($"Reached maximum tries({MaximumGetUniqueReagentTries}) while trying to get unique reagent for SCP-330 Candy with bowl {ToPrettyString(ent)}");
                return FallbackReagent;
            }

            reagent = _random.Pick(ent.Comp.AvailableReagents);

        } while (ent.Comp.CandyEffects.ContainsValue(reagent));

        return reagent;
    }
}
