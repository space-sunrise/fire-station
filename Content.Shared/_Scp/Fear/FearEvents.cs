using Content.Shared._Scp.Fear.Components;

namespace Content.Shared._Scp.Fear;

public sealed class FearCalmDownAttemptEvent(FearState newState) : CancellableEntityEventArgs
{
    public readonly FearState NewState = newState;
}

[ByRefEvent]
public record struct FearStateChangedEvent(FearState NewState, FearState OldState)
{
    public readonly FearState NewState = NewState;
    public readonly FearState OldState = OldState;
}
