using Content.Shared.Chemistry.Components;

namespace Content.Shared._Scp.Other.Events;

/// <summary>
/// Уведомляет SCP-системы об успешном поглощении одной порции содержимого.
/// </summary>
[ByRefEvent]
public readonly record struct ScpIngestionConsumedEvent(EntityUid? Food, Solution ConsumedSolution);
