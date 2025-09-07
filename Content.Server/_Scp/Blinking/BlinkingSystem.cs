using Content.Server.Actions;
using Content.Shared._Scp.Blinking;
using Content.Shared.GameTicking;

namespace Content.Server._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlinkableComponent, EntityUnpausedEvent>(OnUnpaused);

        SubscribeLocalEvent<BlinkableComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnMapInit(Entity<BlinkableComponent> ent, ref MapInitEvent _)
    {
        _actions.AddAction(ent, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
        _actions.SetUseDelay(ent.Comp.EyeToggleActionEntity, ent.Comp.BlinkingDuration);
        Dirty(ent);

        ResetBlink(ent.AsNullable(), predicted: false);
    }

    private void OnUnpaused(Entity<BlinkableComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextBlink += args.PausedTime;
        Dirty(ent);
    }

    private void OnPlayerSpawn(Entity<BlinkableComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        RaiseNetworkEvent(new PlayerOpenEyesAnimation(GetNetEntity(ent)));
    }
}
