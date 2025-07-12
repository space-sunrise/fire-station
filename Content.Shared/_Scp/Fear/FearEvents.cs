using Content.Shared._Scp.Fear.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Fear;

public sealed class FearCalmDownAttemptEvent : CancellableEntityEventArgs;

[ByRefEvent]
public record struct FearStateChangedEvent(FearState OldState)
{
    public FearState OldState = OldState;
}
