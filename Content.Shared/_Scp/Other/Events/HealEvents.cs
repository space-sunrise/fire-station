namespace Content.Shared._Scp.Other.Events;

[ByRefEvent]
public record struct HealingRelayEvent(EntityUid? Entity = null);
