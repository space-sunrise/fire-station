using System.Threading;
using Content.Server.Popups;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Blinking.ReducedBlinking;
using Content.Shared.GameTicking;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Blinking.ReducedBlinking;

public sealed class ReducedBlinkingSystem : SharedReducedBlinkingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    private CancellationTokenSource _token = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveReducedBlinkingUserComponent, ComponentStartup>(OnUserStartup);
        SubscribeLocalEvent<ActiveReducedBlinkingUserComponent, ComponentShutdown>(OnUserShutdown);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => RecreateToken());
    }

    private void OnUserStartup(Entity<ActiveReducedBlinkingUserComponent> ent, ref ComponentStartup _)
    {
        if (!TryComp<BlinkableComponent>(ent, out var blinkable))
            return;

        blinkable.BlinkingInterval += ent.Comp.BlinkingBonusTime;
        DirtyField(ent.Owner, blinkable, nameof(BlinkableComponent.BlinkingInterval));

        Timer.Spawn(ent.Comp.Duration, () => RemComp<ActiveReducedBlinkingUserComponent>(ent), _token.Token);
    }

    private void OnUserShutdown(Entity<ActiveReducedBlinkingUserComponent> ent, ref ComponentShutdown _)
    {
        if (!TryComp<BlinkableComponent>(ent, out var blinkable))
            return;

        blinkable.BlinkingInterval -= ent.Comp.BlinkingBonusTime;
        DirtyField(ent.Owner, blinkable, nameof(BlinkableComponent.BlinkingInterval));

        _popup.PopupEntity(Loc.GetString("eye-droplets-end"), ent, ent);
    }

    private void RecreateToken()
    {
        _token.Cancel();
        _token = new();
    }
}
