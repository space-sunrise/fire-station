using System.Linq;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Scp173;
using Content.Shared.Alert;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

public abstract class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedEyeClosingSystem _closingSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly TimeSpan _blinkingInterval = TimeSpan.FromSeconds(8);
    private readonly TimeSpan _blinkingDuration = TimeSpan.FromSeconds(2.4);

    private static readonly TimeSpan BlinkingIntervalVariance = TimeSpan.FromSeconds(4);


    // TODO: Рефактор моргания с целью сделать как в контеймент бриче юнити.


    public bool IsBlind(Entity<BlinkableComponent?> ent, bool useTimeCompensation = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        // Специально для сцп173. Он должен начинать остановку незадолго до того, как у людей откроются глаза
        // Это поможет избежать эффекта "скольжения", когда игрок не может двигаться, но тело все еще летит вперед на инерции
        // Благодаря этому волшебному числу в 0.7 секунды при открытии глаз 173 должен будет уже остановиться. Возможно стоит немного увеличить
        if (useTimeCompensation)
            return _gameTiming.CurTime + TimeSpan.FromSeconds(0.7) < ent.Comp.BlinkEndTime;

        return _gameTiming.CurTime < ent.Comp.BlinkEndTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            var blinkableEntity = (uid, blinkableComponent);

            if (!IsScp173Nearby(uid))
            {
                ResetBlink(blinkableEntity, false);
                continue;
            }

            // TODO: перенести на ивенты и вынести отсюда, этож каждый тик мертвые ресетят себя
            if (_mobState.IsIncapacitated(uid))
            {
                ResetBlink(blinkableEntity, false);
                continue;
            }

            // TODO: Перенести на ивенты
            if (_closingSystem.AreEyesClosed(uid))
            {
                ResetBlink(blinkableEntity);
                continue;
            }

            var currentTime = _gameTiming.CurTime;

            // TODO: Пофиксить, что первый раз все моргают одновременно
            if (currentTime >= blinkableComponent.NextBlink)
            {
                Blink(blinkableEntity);
            }

            UpdateAlert(blinkableEntity);
        }
    }

    private void Blink(Entity<BlinkableComponent> ent)
    {
        ent.Comp.BlinkEndTime = _gameTiming.CurTime + _blinkingDuration;
        Dirty(ent);

        if (_gameTiming.IsFirstTimePredicted)
            PlayBlinkSound(ent);

        var variance = _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds;

        SetNextBlink((ent.Owner, ent.Comp), _blinkingInterval, variance);
    }

    /// <summary>
    /// Задает время следующего моргания персонажа
    /// </summary>
    /// <remarks>Выделил в отдельный метод, чтобы манипулировать этим извне системы</remarks>
    /// <param name="ent">Моргающий</param>
    /// <param name="interval">Через сколько будет следующее моргание</param>
    /// <param name="variance">Плюс-минус время следующего моргания, чтобы вся станция не моргала в один такт</param>
    public void SetNextBlink(Entity<BlinkableComponent?> ent, TimeSpan interval, double variance = 0)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.NextBlink = _gameTiming.CurTime + interval + TimeSpan.FromSeconds(variance) + TimeSpan.FromSeconds(ent.Comp.AdditionalBlinkingTime);
        ent.Comp.AdditionalBlinkingTime = 0f;

        Dirty(ent);
    }

    private bool IsScp173Nearby(EntityUid uid)
    {
        var entities = _scpHelpers.GetAll<Scp173Component>().ToHashSet();

        if (entities.Count == 0)
            return false;

        if (!entities.Any(scp => _examine.InRangeUnOccluded(uid, scp, 12f, ignoreInsideBlocker: false)))
            return false;

        return true;
    }

    protected void UpdateAlert(Entity<BlinkableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var currentTime = _gameTiming.CurTime;

        if (IsBlind(ent))
        {
            _alertsSystem.ShowAlert(ent, ent.Comp.BlinkingAlert, 4);
            return;
        }

        var timeToNextBlink = ent.Comp.NextBlink - currentTime;
        var severity = (short)Math.Clamp(4 - timeToNextBlink.TotalSeconds / (float)(_blinkingInterval.TotalSeconds - _blinkingDuration.TotalSeconds) * 4, 0, 4);

        _alertsSystem.ShowAlert(ent, ent.Comp.BlinkingAlert, severity);
    }

    public void ResetBlink(Entity<BlinkableComponent?> ent, bool useVariance = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Если useVariance == false, то variance = 0
        var variance = useVariance ? _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds : 0;
        SetNextBlink(ent, _blinkingInterval, variance);

        UpdateAlert(ent);
    }

    public bool CanCloseEyes(Entity<BlinkableComponent?> ent)
    {
        return !IsBlind(ent);
    }

    // TODO: Объединить с Blink()
    public void ForceBlind(Entity<BlinkableComponent?> ent, TimeSpan duration)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (_mobState.IsIncapacitated(ent))
            return;

        ent.Comp.BlinkEndTime = _gameTiming.CurTime + duration;
        Dirty(ent);

        if (_gameTiming.IsFirstTimePredicted)
            PlayBlinkSound(ent);

        // Set next blink slightly after forced blindness ends
        SetNextBlink(ent, duration + TimeSpan.FromSeconds(1));

        UpdateAlert(ent);
    }

    protected virtual void PlayBlinkSound(EntityUid uid) { }
}

