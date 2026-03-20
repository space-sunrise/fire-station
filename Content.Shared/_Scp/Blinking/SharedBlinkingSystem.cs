using System.Linq;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Scp173;
using Content.Shared._Scp.Watching;
using Content.Shared._Sunrise.Random;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Blinking;

// TODO: Избавиться от членения на EyeClosing и Blinking.
// Они слишком сильно переплетаются, чтобы их так разделять.
// Вместо этого разделить систему на апдейт + обработку ивентов | API + хелперы + ивенты
public abstract partial class SharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly RandomPredictedSystem _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected EntityQuery<BlinkableComponent> BlinkableQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, EntityOpenedEyesEvent>(OnOpenedEyes);
        SubscribeLocalEvent<BlinkableComponent, EntityClosedEyesEvent>(OnClosedEyes);

        SubscribeLocalEvent<BlinkableComponent, MobStateChangedEvent>(OnMobStateChanged);

        InitializeEyeClosing();

        BlinkableQuery = GetEntityQuery<BlinkableComponent>();
    }

    #region Event handlers

    /// <summary>
    /// Происходит при закрытии глаз.
    /// Устанавливает время, когда глаза будут открыты
    /// </summary>
    protected virtual void OnClosedEyes(Entity<BlinkableComponent> ent, ref EntityClosedEyesEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var duration = args.CustomBlinkDuration ?? ent.Comp.BlinkingDuration;
        ent.Comp.BlinkEndTime = _timing.CurTime + duration;

        _actions.SetCooldown(ent.Comp.EyeToggleActionEntity, duration);

        // Если глаза были закрыты вручную игроком, то нам не нужно, чтобы они были автоматически открыты
        // Поэтому время, когда глаза будут открыты устанавливается максимальное
        // И игрок должен будет сам вручную их открыть.
        if (ent.Comp.ManuallyClosed)
            ent.Comp.BlinkEndTime = _timing.CurTime + TimeSpan.FromDays(3);

        // Так как персонажи моргают на протяжении всего времени, то для удобства игрока мы
        // Не добавляем никакие эффекты, если рядом нет SCP использующего механику зрения
        if (ent.Comp.ManuallyClosed || IsScpNearby(ent))
            _blindable.UpdateIsBlind(ent.Owner);

        if (_net.IsServer)
            DirtyField(ent.AsNullable(), nameof(BlinkableComponent.BlinkEndTime));
    }

    protected virtual void OnOpenedEyes(Entity<BlinkableComponent> ent, ref EntityOpenedEyesEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Если мы закрывали глаза вручную, то после открытия у нас до следующего автоматического моргания будет сломан алерт
        // Потому что BlinkEndTime равняется 9999999999. И поэтому после открытия глаз я записываю его сюда
        ent.Comp.BlinkEndTime = _timing.CurTime;
        if (_net.IsServer)
            DirtyField(ent.AsNullable(), nameof(BlinkableComponent.BlinkEndTime));

        // Задаем время следующего моргания
        var variance = GetBlinkVariance(ent);
        SetNextBlink(ent.AsNullable(), args.CustomNextTimeBlinkInterval ?? ent.Comp.BlinkingInterval, variance);

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

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<BlinkableComponent>();
        while (query.MoveNext(out var uid, out var blinkableComponent))
        {
            var blinkableEntity = (uid, blinkableComponent);

            if (TryOpenEyes(blinkableEntity))
                continue;

            TryBlink(blinkableEntity);
        }
    }

    #region Blink logic

    private bool TryBlink(Entity<BlinkableComponent?> ent, TimeSpan? customDuration = null)
    {
        if (!BlinkableQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (_timing.CurTime < ent.Comp.NextBlink)
            return false;

        if (!TrySetEyelids(ent, EyesState.Closed, customBlinkDuration: customDuration))
            return false;

        return true;
    }

    /// <summary>
    /// Задает время следующего моргания персонажа
    /// </summary>
    /// <remarks>Выделил в отдельный метод, чтобы манипулировать этим извне системы</remarks>
    /// <param name="ent">Моргающий</param>
    /// <param name="interval">Через сколько будет следующее моргание</param>
    /// <param name="variance">Плюс-минус время следующего моргания, чтобы вся станция не моргала в один такт</param>
    /// <param name="predicted">Предугадывается ли клиентом этот вызов метода? Если нет, отправляет клиенту стейт с сервера.</param>
    public void SetNextBlink(Entity<BlinkableComponent?> ent, TimeSpan interval, TimeSpan? variance = null, bool predicted = true)
    {
        if (!BlinkableQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!variance.HasValue)
            variance = TimeSpan.Zero;

        DebugTools.Assert(interval >= TimeSpan.Zero, $"Blink interval must be >= 0, got {interval}");

        if (interval < TimeSpan.Zero)
            interval = TimeSpan.Zero;

        ent.Comp.NextBlink = _timing.CurTime + interval + variance.Value + ent.Comp.AdditionalBlinkingTime;
        ent.Comp.AdditionalBlinkingTime = TimeSpan.Zero;

        if (!predicted)
            DirtyFields(ent, null, nameof(BlinkableComponent.NextBlink), nameof(BlinkableComponent.AdditionalBlinkingTime));
    }

    public void ResetBlink(Entity<BlinkableComponent?> ent, bool useVariance = true, bool predicted = true)
    {
        if (!BlinkableQuery.Resolve(ent, ref ent.Comp))
            return;

        // Если useVariance == false, то variance = 0
        var variance = useVariance ? GetBlinkVariance((ent.Owner, ent.Comp)) : TimeSpan.Zero;
        SetNextBlink(ent, ent.Comp.BlinkingInterval, variance, predicted);
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
        if (!BlinkableQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        // Специально для сцп173. Он должен начинать остановку незадолго до того, как у людей откроются глаза
        // Это поможет избежать эффекта "скольжения", когда игрок не может двигаться, но тело все еще летит вперед на инерции
        // Благодаря этому волшебному числу в 0.7 секунды при открытии глаз 173 должен будет уже остановиться. Возможно стоит немного увеличить
        if (useTimeCompensation)
            return _timing.CurTime + TimeSpan.FromSeconds(0.7) < ent.Comp.BlinkEndTime;

        return _timing.CurTime < ent.Comp.BlinkEndTime;
    }

    public void ForceBlind(Entity<BlinkableComponent?> ent, TimeSpan duration, bool predicted = true)
    {
        if (_mobState.IsIncapacitated(ent))
            return;

        if (!BlinkableQuery.Resolve(ent, ref ent.Comp))
            return;

        // Если у персонажа уже закрыты глаза, то обновляем время
        if (ent.Comp.State == EyesState.Closed)
        {
            ent.Comp.BlinkEndTime = _timing.CurTime + duration;

            DirtyField(ent, nameof(BlinkableComponent.BlinkEndTime));

            _actions.SetCooldown(ent.Comp.EyeToggleActionEntity, duration);

            return;
        }

        TrySetEyelids(ent.Owner, EyesState.Closed, false, predicted, true, duration);
    }

    private TimeSpan GetBlinkVariance(Entity<BlinkableComponent> ent)
    {
        var time = _random.NextFloatForEntity(ent, 0, (float)ent.Comp.BlinkingIntervalVariance.TotalSeconds);
        return TimeSpan.FromSeconds(time);
    }

    #endregion

    /// <summary>
    /// Проверяет, есть ли рядом с игроком Scp, использующий механики зрения
    /// <remarks>
    /// На данный момент это SCP-173 и SCP-096
    /// </remarks>
    /// </summary>
    /// <param name="player">Игрок, которого мы проверяем</param>
    protected bool IsScpNearby(EntityUid player)
    {
        // Получаем всех Scp с механиками зрения, которые видят игрока
        using var scp173List = ListPoolEntity<Scp173Component>.Rent();
        if (!_watching.TryGetAllEntitiesVisibleTo(player, scp173List.Value, flags: LookupFlags.Dynamic | LookupFlags.Approximate))
            return false;

        return scp173List.Value.Any(e => _watching.CanBeWatched(player, e));
    }
}

[ByRefEvent]
public readonly record struct EntityOpenedEyesEvent(
    bool Manual = false,
    bool UseEffects = false,
    TimeSpan? CustomNextTimeBlinkInterval = null);

[ByRefEvent]
public readonly record struct EntityClosedEyesEvent(
    bool Manual = false,
    bool UseEffects = false,
    TimeSpan? CustomBlinkDuration = null);

[Serializable, NetSerializable]
public sealed class EntityEyesStateChanged(EyesState oldState, EyesState newState, bool manual = false, bool useEffects = false, NetEntity? netEntity = null) : EntityEventArgs
{
    public readonly EyesState OldState = oldState;
    public readonly EyesState NewState = newState;
    public readonly bool Manual = manual;
    public readonly bool UseEffects = useEffects;
    public readonly NetEntity? NetEntity = netEntity;
}

[Serializable, NetSerializable]
public sealed class PlayerOpenEyesAnimation(NetEntity netEntity) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
}
