using Content.Shared._Scp.Blinking;
using Content.Shared.Alert;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly EyeClosingSystem _closingSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly TimeSpan BlinkingInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan BlinkingDuration = TimeSpan.FromSeconds(0.3);  // 1.5 секунды моргание - отвлекает

    private TimeSpan _nextTick = TimeSpan.Zero;
    private readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(1);

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextTick > _gameTiming.CurTime)
            return;

        _nextTick += _refreshCooldown;

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            if (_mobState.IsIncapacitated(uid))
                continue;

            if (_closingSystem.AreEyesClosed(uid))
            {
                blinkableComponent.NextBlink = _gameTiming.CurTime + BlinkingInterval;
                continue;
            }

            var currentTime = _gameTiming.CurTime;

            if (currentTime >= blinkableComponent.NextBlink)
            {
                Blink(uid, blinkableComponent);
            }

            UpdateAlert(uid, blinkableComponent);
        }
    }

    private void Blink(EntityUid uid, BlinkableComponent component)
    {
        component.NextBlink = _gameTiming.CurTime + BlinkingInterval;
        component.BlinkEndTime = _gameTiming.CurTime + BlinkingDuration;

        Dirty(uid, component);
    }

    private void UpdateAlert(EntityUid uid, BlinkableComponent component)
    {
        var currentTime = _gameTiming.CurTime;

        if (IsBlind(uid, component))
        {
            _alertsSystem.ShowAlert(uid, component.BlinkingAlert, 4);
            return;
        }

        var timeToNextBlink = component.NextBlink - currentTime;
        var severity = (short)Math.Clamp(4 - timeToNextBlink.TotalSeconds / (float)(BlinkingInterval.TotalSeconds - BlinkingDuration.TotalSeconds) * 4, 0, 4);

        _alertsSystem.ShowAlert(uid, component.BlinkingAlert, severity);
    }

    public override void ResetBlink(EntityUid uid, BlinkableComponent component)
    {
        base.ResetBlink(uid, component);
        if (component.NextBlink == TimeSpan.Zero)  // Иначе вся станция будет моргать одновременно
        {
            component.NextBlink = _gameTiming.CurTime + _random.NextFloat() * BlinkingInterval;
        }
        else
        {
            component.NextBlink = _gameTiming.CurTime + BlinkingInterval;
        }
        Dirty(uid, component);
    }
}
