using Content.Server.Flash;
using Content.Shared._Scp.Blinking;
using Content.Shared.Flash;

namespace Content.Server._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, EntityUnpausedEvent>(OnUnpaused);

        SubscribeLocalEvent<BlinkableComponent, FlashAttemptEvent>(OnFlash);
    }

    private void OnUnpaused(Entity<BlinkableComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextBlink += args.PausedTime;
        Dirty(ent);
    }

    private static void OnFlash(Entity<BlinkableComponent> ent, ref FlashAttemptEvent args)
    {
        if (ent.Comp.State == EyesState.Closed)
            args.Cancelled = true;
    }
}
