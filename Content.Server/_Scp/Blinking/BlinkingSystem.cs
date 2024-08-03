using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Alert;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly EyeClosingSystem _closingSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public static TimeSpan BlinkingInterval = TimeSpan.FromSeconds(15);
    public static TimeSpan BlinkingDuration = TimeSpan.FromSeconds(2);

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

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            if (!IsScp173Nearby(uid))
            {
                ResetBlink(uid, blinkableComponent);
                continue;
            }

            if (_mobState.IsIncapacitated(uid))
            {
                ResetBlink(uid, blinkableComponent);
                continue;
            }

            if (_closingSystem.AreEyesClosed(uid))
            {
                ResetBlink(uid, blinkableComponent);
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

    private bool IsScp173Nearby(EntityUid uid)
    {
        var entities = GetScp173().ToList();
        return entities.Count != 0 && entities.Any(scp => _examine.InRangeUnOccluded(uid, scp, 12f));
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
        component.NextBlink = _gameTiming.CurTime + BlinkingInterval;
        Dirty(uid, component);

        UpdateAlert(uid, component);
    }

    public IEnumerable<Entity<Scp173Component>> GetScp173()
    {
        var query = EntityManager.AllEntityQueryEnumerator<Scp173Component>();
        while (query.MoveNext(out var uid, out var component))
        {
            yield return (uid, component);
        }
    }
}
