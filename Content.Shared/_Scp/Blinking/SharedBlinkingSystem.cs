using System.Linq;
using Content.Shared._Scp.Scp173;
using Content.Shared.Alert;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

public abstract class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EyeClosingSystem _closingSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly TimeSpan _blinkingInterval = TimeSpan.FromSeconds(8);
    private readonly TimeSpan _blinkingDuration = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan BlinkingIntervalVariance = TimeSpan.FromSeconds(4);


    // TODO: Рефактор моргания с целью сделать как в контеймент бриче юнити.
    private readonly SoundSpecifier _blinkingStartSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/start.ogg");
    private readonly SoundSpecifier _blinkingEndSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/end.ogg");

    private readonly SoundSpecifier _blinkSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/blink.ogg");

    public bool IsBlind(EntityUid uid, BlinkableComponent? component = null, bool useTimeCompensation = false)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        // Специально для сцп173. Он должен начинать остановку незадолго до того, как у людей откроются глаза
        // Это поможет избежать эффекта "скольжения", когда игрок не может двигаться, но тело все еще летит вперед на инерции
        // Благодаря этому волшебному числу в 0.7 секунды при открытии глаз 173 должен будет уже остановиться. Возможно стоит немного увеличить
        if (useTimeCompensation)
            return _gameTiming.CurTime + TimeSpan.FromSeconds(0.7) < component.BlinkEndTime;

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

        if (_gameTiming.IsFirstTimePredicted && _player.LocalEntity == uid)
            _audio.PlayGlobal(_blinkSound, Filter.Local(), true);

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
