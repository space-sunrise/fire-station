using System.Linq;
using Content.Shared._Scp.Scp173;
using Content.Shared.Alert;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

public abstract class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EyeClosingSystem _closingSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly TimeSpan _blinkingInterval = TimeSpan.FromSeconds(8);
    private readonly TimeSpan _blinkingDuration = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan BlinkingIntervalVariance = TimeSpan.FromSeconds(4);

    public bool IsBlind(EntityUid uid, BlinkableComponent? component = null, bool useTimeCompensation = false)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (_net.IsClient && useTimeCompensation)
        {
            if (_gameTiming.CurTime < component.BlinkEndTime)
                return _gameTiming.CurTime < component.BlinkEndTime + TimeSpan.FromTicks(10);
            else
                return _gameTiming.CurTime + TimeSpan.FromTicks(10) < component.BlinkEndTime;
        }

        return _gameTiming.CurTime < component.BlinkEndTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            if (!IsScp173Nearby(uid))
            {
                ResetBlink(uid, blinkableComponent, false);
                continue;
            }

            // TODO: перенести на ивенты и вынести отсюда, этож каждый тик мертвые ресетят себя
            if (_mobState.IsIncapacitated(uid))
            {
                ResetBlink(uid, blinkableComponent, false);
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
        component.BlinkEndTime = _gameTiming.CurTime + _blinkingDuration;
        Dirty(uid, component);

        var variance = _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds;

        SetNextBlink(uid, component, _blinkingInterval, variance);
    }

    /// <summary>
    /// Задает время следующего моргания персонажа
    /// </summary>
    /// <remarks>Выделил в отдельный метод, чтобы манипулировать этим извне системы</remarks>
    /// <param name="uid">Моргающий</param>
    /// <param name="component">Компонент моргания</param>
    /// <param name="interval">Через сколько будет следующее моргание</param>
    /// <param name="variance">Плюс минус время следующего моргания, чтобы вся станция не моргала в один такт</param>
    public void SetNextBlink(EntityUid uid, BlinkableComponent component, TimeSpan interval, double variance = 0)
    {
        component.NextBlink = _gameTiming.CurTime + interval + TimeSpan.FromSeconds(variance) + TimeSpan.FromSeconds(component.AdditionalBlinkingTime);
        component.AdditionalBlinkingTime = 0f;

        Dirty(uid, component);
    }

    private bool IsScp173Nearby(EntityUid uid)
    {
        var entities = GetScp173().ToList();
        return entities.Count != 0 && entities.Any(scp => _examine.InRangeUnOccluded(uid, scp, 12f, ignoreInsideBlocker:false));
    }

    protected virtual void UpdateAlert(EntityUid uid, BlinkableComponent component)
    {
        var currentTime = _gameTiming.CurTime;

        if (IsBlind(uid, component))
        {
            _alertsSystem.ShowAlert(uid, component.BlinkingAlert, 4);
            return;
        }

        var timeToNextBlink = component.NextBlink - currentTime;
        var severity = (short)Math.Clamp(4 - timeToNextBlink.TotalSeconds / (float)(_blinkingInterval.TotalSeconds - _blinkingDuration.TotalSeconds) * 4, 0, 4);

        _alertsSystem.ShowAlert(uid, component.BlinkingAlert, severity);
    }

    public void ResetBlink(EntityUid uid, BlinkableComponent? component = null, bool useVariance = true)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // Если useVariance == false, то variance = 0
        var variance = useVariance ? _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds : 0;
        SetNextBlink(uid, component, _blinkingInterval, variance);

        UpdateAlert(uid, component);
    }

    public bool CanCloseEyes(EntityUid uid)
    {
        if (!TryComp<BlinkableComponent>(uid, out var blinkableComponent))
            return false;

        return !IsBlind(uid, blinkableComponent);
    }

    public void ForceBlind(EntityUid uid, BlinkableComponent component, TimeSpan duration)
    {
        if (_mobState.IsIncapacitated(uid))
            return;

        component.BlinkEndTime = _gameTiming.CurTime + duration;
        Dirty(uid, component);

        // Set next blink slightly after forced blindness ends
        SetNextBlink(uid, component, duration + TimeSpan.FromSeconds(1));

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
