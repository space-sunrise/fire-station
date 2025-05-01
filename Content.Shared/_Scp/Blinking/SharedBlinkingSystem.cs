﻿using System.Linq;
using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.Scp173;
using Content.Shared._Scp.Watching;
using Content.Shared.Alert;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Blinking;

public abstract partial class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan BlinkingInterval = TimeSpan.FromSeconds(8f);
    private static readonly TimeSpan BlinkingDuration = TimeSpan.FromSeconds(2.4f);

    private static readonly TimeSpan BlinkingIntervalVariance = TimeSpan.FromSeconds(4f);

    public override void Initialize()
    {
        base.Initialize();

        #region Blinking

        SubscribeLocalEvent<BlinkableComponent, EntityOpenedEyesEvent>(OnOpenedEyes);
        SubscribeLocalEvent<BlinkableComponent, EntityClosedEyesEvent>(OnClosedEyes);

        SubscribeLocalEvent<BlinkableComponent, MobStateChangedEvent>(OnMobStateChanged);

        #endregion

        #region Eye closing

        SubscribeLocalEvent<BlinkableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlinkableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlinkableComponent, ToggleEyesActionEvent>(OnToggleAction);
        SubscribeLocalEvent<BlinkableComponent, CanSeeAttemptEvent>(OnTrySee);

        SubscribeLocalEvent<HumanoidAppearanceComponent, EntityClosedEyesEvent>(OnHumanoidClosedEyes);
        SubscribeLocalEvent<HumanoidAppearanceComponent, EntityOpenedEyesEvent>(OnHumanoidOpenedEyes);

        #endregion
    }

    #region Event handlers

    /// <summary>
    /// Происходит при закрытии глаз.
    /// Устанавливает время, когда глаза будут открыты
    /// </summary>
    private void OnClosedEyes(Entity<BlinkableComponent> ent, ref EntityClosedEyesEvent args)
    {
        var duration = args.CustomBlinkDuration ?? BlinkingDuration;
        ent.Comp.BlinkEndTime = _timing.CurTime + duration;

        _actions.SetUseDelay(ent.Comp.EyeToggleActionEntity, duration);

        // Если глаза были закрыты вручную игроком, то нам не нужно, чтобы они были автоматически открыты
        // Поэтому время, когда глаза будут открыты устанавливается максимальное
        // И игрок должен будет сам вручную их открыть.
        if (ent.Comp.ManuallyClosed)
            ent.Comp.BlinkEndTime = TimeSpan.MaxValue;

        // Так как персонажи моргают на протяжении всего времени, то для удобства игрока мы
        // Не добавляем никакие эффекты, если рядом нет SCP использующего механику зрения
        if (ent.Comp.ManuallyClosed || IsScpNearby(ent))
            _blindable.UpdateIsBlind(ent.Owner);

        Dirty(ent);
    }

    private void OnOpenedEyes(Entity<BlinkableComponent> ent, ref EntityOpenedEyesEvent args)
    {
        // Если мы закрывали глаза вручную, то после открытия у нас до следующего автоматического моргания будет сломан алерт
        // Потому что BlinkEndTime равняется 9999999999. И поэтому после открытия глаз я записываю его сюда
        ent.Comp.BlinkEndTime = _timing.CurTime;
        Dirty(ent);

        // Задаем время следующего моргания
        var variance = _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds;
        SetNextBlink((ent.Owner, ent.Comp), args.CustomNextTimeBlinkInterval ?? BlinkingInterval, variance);

        // Как только глаза открыты, мы проверяем, слепы ли мы
        // Если мы ранее были слепы из-за наличия эффектов, то здесь они уберутся
        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnMobStateChanged(Entity<BlinkableComponent> ent, ref MobStateChangedEvent args)
    {
        CloseEyesIfIncapacitated(ent, ref args);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            var blinkableEntity = (uid, blinkableComponent);

            UpdateAlert(blinkableEntity);

            TryOpenEyes(blinkableEntity);
            TryBlink(blinkableEntity);
        }
    }

    #region Blink logic

    private bool TryBlink(Entity<BlinkableComponent?> ent, TimeSpan? customDuration = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (ent.Comp.State == EyesState.Closed)
            return false;

        if (_timing.CurTime < ent.Comp.NextBlink)
            return false;

        if (_mobState.IsIncapacitated(ent))
            return false;

        TrySetEyelids(ent.Owner, EyesState.Closed, customBlinkDuration: customDuration);
        return true;
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

        ent.Comp.NextBlink = _timing.CurTime + interval + TimeSpan.FromSeconds(variance) + TimeSpan.FromSeconds(ent.Comp.AdditionalBlinkingTime);
        ent.Comp.AdditionalBlinkingTime = 0f;

        Dirty(ent);
    }

    public void ResetBlink(Entity<BlinkableComponent?> ent, bool useVariance = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Если useVariance == false, то variance = 0
        var variance = useVariance ? _random.NextDouble() * BlinkingIntervalVariance.TotalSeconds * 2 - BlinkingIntervalVariance.TotalSeconds : 0;
        SetNextBlink(ent, BlinkingInterval, variance);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Проверяет, слеп ли человек в данный момент
    /// <remarks>
    /// Это не то же самое, что и проверка на закрыты ли глаза
    /// Здесь используется проверка по времени до конца моргания и метод компенсации времени
    /// </remarks>
    /// </summary>
    public bool IsBlind(Entity<BlinkableComponent?> ent, bool useTimeCompensation = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        // Специально для сцп173. Он должен начинать остановку незадолго до того, как у людей откроются глаза
        // Это поможет избежать эффекта "скольжения", когда игрок не может двигаться, но тело все еще летит вперед на инерции
        // Благодаря этому волшебному числу в 0.7 секунды при открытии глаз 173 должен будет уже остановиться. Возможно стоит немного увеличить
        if (useTimeCompensation)
            return _timing.CurTime + TimeSpan.FromSeconds(0.7) < ent.Comp.BlinkEndTime;

        return _timing.CurTime < ent.Comp.BlinkEndTime;
    }

    public void ForceBlind(Entity<BlinkableComponent?> ent, TimeSpan duration)
    {
        if (_mobState.IsIncapacitated(ent))
            return;

        TrySetEyelids(ent.Owner, EyesState.Closed, customBlinkDuration: duration);
    }

    #endregion

    /// <summary>
    /// Актуализирует иконку моргания справа у панели чата игрока
    /// </summary>
    protected void UpdateAlert(Entity<BlinkableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Если в данный момент глаза закрыты, то выставляем иконку с закрытым глазом
        if (IsBlind(ent))
        {
            _alerts.ShowAlert(ent, ent.Comp.BlinkingAlert, 4);
            return;
        }

        var timeToNextBlink = ent.Comp.NextBlink - _timing.CurTime;
        var severity = (short)Math.Clamp(4 - timeToNextBlink.TotalSeconds / (float)(BlinkingInterval.TotalSeconds - BlinkingDuration.TotalSeconds) * 4, 0, 4);

        _alerts.ShowAlert(ent, ent.Comp.BlinkingAlert, severity);
    }

    /// <summary>
    /// Проверяет, есть ли рядом с игроком Scp, использующий механики зрения
    /// <remarks>
    /// На данный момент это SCP-173 и SCP-096
    /// </remarks>
    /// </summary>
    /// <param name="player">Игрок, которого мы проверяем</param>
    /// <returns></returns>
    protected bool IsScpNearby(EntityUid player)
    {
        // Получаем всех Scp с механиками зрения, которые видят игрока
        var allScp173InView = _watching.GetAllVisibleTo<Scp173Component>(player);
        var allScp096InView = _watching.GetAllVisibleTo<Scp096Component>(player);

        return allScp173InView.Any() || allScp096InView.Any();
    }
}

[Serializable, NetSerializable]
public sealed class EntityOpenedEyesEvent(TimeSpan? customNextTimeBlinkInterval = null) : EntityEventArgs
{
    public TimeSpan? CustomNextTimeBlinkInterval = customNextTimeBlinkInterval;
}

[Serializable, NetSerializable]
public sealed class EntityClosedEyesEvent(bool manual, TimeSpan? customBlinkDuration = null) : EntityEventArgs
{
    public bool Manual = manual;
    public TimeSpan? CustomBlinkDuration = customBlinkDuration;
};
