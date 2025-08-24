using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;

namespace Content.Shared._Scp.Blinking;

public abstract partial class SharedBlinkingSystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private void InitializeEyeClosing()
    {
        SubscribeLocalEvent<BlinkableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlinkableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlinkableComponent, ToggleEyesActionEvent>(OnToggleAction);
        SubscribeLocalEvent<BlinkableComponent, CanSeeAttemptEvent>(OnTrySee);

        SubscribeLocalEvent<HumanoidAppearanceComponent, EntityClosedEyesEvent>(OnHumanoidClosedEyes);
        SubscribeLocalEvent<HumanoidAppearanceComponent, EntityOpenedEyesEvent>(OnHumanoidOpenedEyes);

        SubscribeLocalEvent<BlinkableComponent, SleepStateChangedEvent>(OnWakeUp);
        SubscribeLocalEvent<BlinkableComponent, TryingToSleepEvent>(OnTryingSleep);
    }

    #region Event handlers

    private void OnMapInit(Entity<BlinkableComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.EyeToggleActionEntity, ent.Comp.EyeToggleAction);
        Dirty(ent);

        _actions.SetUseDelay(ent.Comp.EyeToggleActionEntity, BlinkingDuration);

        ResetBlink(ent.Owner);
    }

    private void OnShutdown(Entity<BlinkableComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.EyeToggleActionEntity);

        if (!Exists(ent))
            return;

        // Возвращаем цвет глаз на исходный, если в момент удаления компонента они были закрыты
        if (ent.Comp.CachedEyesColor == null)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoidAppearanceComponent))
            return;

        humanoidAppearanceComponent.EyeColor = ent.Comp.CachedEyesColor.Value;
        Dirty(ent.Owner, humanoidAppearanceComponent);
    }

    private void OnToggleAction(Entity<BlinkableComponent> ent, ref ToggleEyesActionEvent args)
    {
        if (args.Handled)
            return;

        // Нельзя закрыть глаза, если нас ослепили.
        // Потому что это приведет к странному багу
        if (HasComp<FlashedComponent>(ent))
            return;

        // Нельзя дергать глазами, пока мы спим
        if (HasComp<SleepingComponent>(ent))
            return;

        var newState = ent.Comp.State == EyesState.Closed ? EyesState.Opened : EyesState.Closed;
        args.Handled = TrySetEyelids((ent.Owner, ent.Comp), newState, true);
    }

    private void OnTrySee(Entity<BlinkableComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.State != EyesState.Closed)
            return;

        if (!ent.Comp.ManuallyClosed && !IsScpNearby(ent))
            return;

        args.Cancel();
    }

    private void OnWakeUp(Entity<BlinkableComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            return;

        TrySetEyelids((ent.Owner, ent.Comp), EyesState.Opened, true);
    }

    private void OnTryingSleep(Entity<BlinkableComponent> ent, ref TryingToSleepEvent args)
    {
        if (args.Cancelled)
            return;

        TrySetEyelids((ent.Owner, ent.Comp), EyesState.Closed, true);
    }

    /// <summary>
    /// Создает эффект закрытия глаз на спрайте, путем смены цвета глаз на цвет кожи, но чуть более темный
    /// </summary>
    private void OnHumanoidClosedEyes(Entity<HumanoidAppearanceComponent> ent, ref EntityClosedEyesEvent args)
    {
        if (!TryComp<BlinkableComponent>(ent, out var blinkableComponent))
            return;

        blinkableComponent.CachedEyesColor = ent.Comp.EyeColor;
        ent.Comp.EyeColor = DarkenSkinColor(ent.Comp.SkinColor);

        Dirty(ent);
    }

    /// <summary>
    /// Возвращает цвет глаз на исходный, когда они открываются
    /// </summary>
    private void OnHumanoidOpenedEyes(Entity<HumanoidAppearanceComponent> ent, ref EntityOpenedEyesEvent args)
    {
        if (!TryComp<BlinkableComponent>(ent, out var blinkableComponent))
            return;

        if (blinkableComponent.CachedEyesColor == null)
            return;

        ent.Comp.EyeColor = blinkableComponent.CachedEyesColor.Value;

        Dirty(ent);
    }

    #endregion

    #region API

    /// <summary>
    /// Устанавливает состояние глаз
    /// </summary>
    /// <param name="ent">Сущность, способная моргать</param>
    /// <param name="newState">Устанавливаемое состояние глаз. Закрыть/открыть</param>
    /// <param name="manual">Закрыли ли глаза вручную?</param>
    /// <param name="customBlinkDuration">Если нужно вручную задать время, которое игрок проведет с закрытыми глазами</param>
    public bool TrySetEyelids(Entity<BlinkableComponent?> ent, EyesState newState, bool manual = false, TimeSpan? customBlinkDuration = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State == newState)
            return false;

        if (!CanToggleEyes((ent.Owner, ent.Comp), newState))
            return false;

        SetEyelids((ent.Owner, ent.Comp), newState, manual, customBlinkDuration);

        return true;
    }

    /// <summary>
    /// Проверяет, может ли персонаж в данный момент открыть/закрыть глаза
    /// </summary>
    /// <param name="ent">Сущность, которая пытается открыть/закрыть глаза</param>
    /// <param name="newState">Состояние глаз, которое мы хотим установить</param>
    /// <returns>Может/не может</returns>
    public bool CanToggleEyes(Entity<BlinkableComponent> ent, EyesState newState)
    {
        if (_mobState.IsIncapacitated(ent))
            return false;

        // Если в данный момент глаза закрыты(то есть мы хотим их открыть)
        // То мы должны проверить, имеет ли право персонаж открыть глаза сейчас
        if (newState == EyesState.Opened && !ent.Comp.ManuallyClosed)
            return !IsBlind(ent.AsNullable()); // Можем открыть глаза, если мы не моргаем в данный момент
        else
            return true;
    }

    /// <summary>
    /// Проверяет, закрыты ли глаза у сущности и находится ли она в процессе моргания
    /// </summary>
    /// <param name="ent">Сущность для проверки</param>
    /// <returns>True -> закрыты, False -> открыты</returns>
    public bool AreEyesClosed(Entity<BlinkableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        // Мб одна из проверок лишняя, но ладно пусть будет. Не сильная потеря производительности
        if (!IsBlind(ent) || ent.Comp.State != EyesState.Closed)
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет, закрыты ли у сущности глаза вручную.
    /// </summary>
    public bool AreEyesClosedManually(Entity<BlinkableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (ent.Comp.State != EyesState.Closed)
            return false;

        if (!ent.Comp.ManuallyClosed)
            return false;

        return true;
    }

    #endregion

    private bool TryOpenEyes(Entity<BlinkableComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.BlinkEndTime)
            return false;

        TrySetEyelids(ent.Owner, EyesState.Opened);
        return true;
    }

    /// <summary>
    /// Устанавливает состояние глаз без проверок
    /// </summary>
    /// <param name="ent">Сущность, которой будет установлено состояние</param>
    /// <param name="newState">Новое состояние глаз, которое мы хотим установить</param>
    /// <param name="manual">Устанавливается вручную(игрок нажал на кнопку закрытия глаз)?</param>
    /// <param name="customBlinkDuration">Если нужно использовать какое-то отличное от стандарта время, которое игрок проведет с закрытыми глазами</param>
    /// <remarks>
    /// Поле manual влияет на то, будут ли глаза автоматически открыты после КД.
    /// Если глаза закрыты вручную, то их нужно будет и открывать вручную
    /// </remarks>
    private void SetEyelids(Entity<BlinkableComponent> ent, EyesState newState, bool manual = false, TimeSpan? customBlinkDuration = null)
    {
        ent.Comp.State = newState;
        ent.Comp.ManuallyClosed = manual && newState == EyesState.Closed;
        Dirty(ent);

        if (newState == EyesState.Closed)
            RaiseLocalEvent(ent, new EntityClosedEyesEvent(manual, customBlinkDuration));
        else
            RaiseLocalEvent(ent, new EntityOpenedEyesEvent(customBlinkDuration));

        if (ent.Comp.EyeToggleActionEntity != null)
        {
            // Так как по умолчанию глаза открыты, то для первого переключения мы должны выдать false, чтобы их закрыть
            // Поэтому проверка идет на Opened
            var value = newState == EyesState.Opened;
            _actions.SetToggled(ent.Comp.EyeToggleActionEntity, value);
        }
    }

    /// <summary>
    /// Закрывает глаза персонажу, если он в критическом состоянии или мертв. Мертвые ничего не видят
    /// </summary>
    private void CloseEyesIfIncapacitated(Entity<BlinkableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead && args.NewMobState != MobState.Critical)
            return;

        SetEyelids(ent, EyesState.Closed, true);
    }

    /// <summary>
    /// Вспомогательный метод, который требуется для создания эффект закрытия глаз на спрайте.
    /// Получает исходный цвет кожи и возвращает его более темную версию.
    /// <remarks>
    /// Более темная версия цвета кожи нужна, чтобы не создавалось ощущение, будто глаза пропали
    /// </remarks>
    /// </summary>
    /// <param name="original">Цвет кожи персонажа</param>
    /// <returns>Более темный цвет кожи</returns>
    private static Color DarkenSkinColor(Color original)
    {
        var hsl = Color.ToHsl(original);

        var newLightness = hsl.Z * 0.85f;
        newLightness = Math.Clamp(newLightness, 0f, 1f);

        var newHsl = hsl with { Z = newLightness };

        return Color.FromHsl(newHsl);
    }
}

public sealed partial class ToggleEyesActionEvent : InstantActionEvent;
