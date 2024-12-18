using Content.Shared._Scp.Blinking;

namespace Content.Server._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<BlinkableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlinkableComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnCompInit(Entity<BlinkableComponent> ent, ref ComponentInit args)
    {
        ResetBlink(ent.Owner, ent.Comp);
    }

    private void OnMapInit(Entity<BlinkableComponent> ent, ref MapInitEvent args)
    {
        ResetBlink(ent.Owner, ent.Comp);
    }

    private void OnUnpaused(Entity<BlinkableComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextBlink += args.PausedTime;
        Dirty(ent);
    }
}
