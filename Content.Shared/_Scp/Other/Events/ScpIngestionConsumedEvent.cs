using Content.Shared.Chemistry.Components;

namespace Content.Shared._Scp.Other.Events;

[ByRefEvent]
public readonly record struct ScpIngestionConsumedEvent(EntityUid? Entity, Solution ConsumedSolution);
