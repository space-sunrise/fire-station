using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

public abstract class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public bool IsBlind(EntityUid uid, BlinkableComponent component)
    {
        var currentTime = _gameTiming.CurTime;
        return currentTime < component.BlinkEndTime;
    }

    public virtual void ForceBlind(EntityUid uid, BlinkableComponent component, TimeSpan duration) {}

    public virtual void ResetBlink(EntityUid uid, BlinkableComponent component) {}
}
