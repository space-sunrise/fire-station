using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Shaders;
using Content.Shared._Scp.Shaders.Grain;
using Content.Shared._Scp.Shaders.Highlighting;
using Content.Shared._Scp.Shaders.Vignette;
using Content.Shared._Scp.Watching;
using Content.Shared._Sunrise.Mood;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Fear.Systems;

/// <summary>
/// Система страха.
/// Работает на следующих базовых принципах:
/// Есть два способа взаимодействовать со страхом: повышать уровень страха или приближаться к источнику страха.
/// Приближение к источнику страха сильно усиливает эффекты в зависимости от расстояния до источника.
/// Повышение уровня страха усиливает эффекты, уровень страха понижается со временем.
/// </summary>
public abstract partial class SharedFearSystem : EntitySystem
{
    [Dependency] private readonly SharedHighlightSystem _highlight = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedShaderStrengthSystem _shaderStrength = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string MoodSourceClose = "FearSourceClose";
    private const string MoodFearSourceSeen = "FearSourceSeen";

    private EntityQuery<FearSourceComponent> _fears;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, EntityLookedAtEvent>(OnEntityLookedAt);
        SubscribeLocalEvent<FearComponent, ProximityInRangeTargetEvent>(OnProximityInRange);
        SubscribeLocalEvent<FearComponent, ProximityNotInRangeTargetEvent>(OnProximityNotInRange);

        SubscribeLocalEvent<FearComponent, FearStateChangedEvent>(OnFearStateChanged);

        SubscribeLocalEvent<FearComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FearComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<FearComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Clear());

        InitializeFears();
        InitializeGameplay();
        InitializeTraits();

        _fears = GetEntityQuery<FearSourceComponent>();
    }

    /// <summary>
    /// Обрабатывает событие, когда игрок посмотрел на источник страха.
    /// Повышает уровень страха до указанного у источника уровня.
    /// </summary>
    private void OnEntityLookedAt(Entity<FearComponent> ent, ref EntityLookedAtEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_mobState.IsAlive(ent))
            return;

        // Проверка на видимость.
        // Это нужно, чтобы можно было не пугаться через стекло, например.
        // Это будет использовано, например, у ученых, которые 100 лет видели сцп через стекла и не должны пугаться.
        if (args.BlockerLevel > ent.Comp.SeenBlockerLevel)
            return;

        if (!args.Target.Comp.AlreadyLookedAt.TryGetValue(GetNetEntity(ent), out var lastSeenTime))
            return;

        if (_timing.CurTime < lastSeenTime + ent.Comp.TimeToGetScaredAgainOnLookAt)
            return;

        if (!_fears.TryComp(args.Target, out var source))
            return;

        if (source.UponSeenState == FearState.None)
            return;

        // Если текущий уровень страха выше, чем тот, что мы хотим поставить,
        // то мы не должны его ставить.
        if (ent.Comp.State > source.UponSeenState)
            return;

        // Если в списке фобий персонажа нет фобии, которой является источник, то мы ее не боимся
        if (source.PhobiaType.HasValue && !ent.Comp.Phobias.Contains(source.PhobiaType.Value))
            return;

        if (!TrySetFearLevel(ent.AsNullable(), source.UponSeenState))
            return;

        AddNegativeMoodEffect(ent, MoodFearSourceSeen);
        HighLightAllVisibleFears(ent);
    }

    /// <summary>
    /// Обрабатывает событие, когда игрок находится вблизи с источником страха.
    /// Нужен, чтобы включить разные страшные эффекты.
    /// </summary>
    private void OnProximityInRange(Entity<FearComponent> ent, ref ProximityInRangeTargetEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_mobState.IsAlive(ent))
            return;

        // Проверка на видимость.
        // Это нужно, чтобы можно было не пугаться через стекло, например.
        // Это будет использовано, например, у ученых, которые 100 лет видели сцп через стекла и не должны пугаться.
        if (args.Type > ent.Comp.ProximityBlockerLevel)
            return;

        if (!_fears.TryComp(args.Receiver, out var source))
            return;

        if (source.UponComeCloser == FearState.None)
            return;

        // Если в списке фобий персонажа нет фобии, которой является источник, то мы ее не боимся
        if (source.PhobiaType.HasValue && !ent.Comp.Phobias.Contains(source.PhobiaType.Value))
            return;

        // Проверка на зрение, чтобы можно было закрыть глазки и было не страшно
        if (!_watching.SimpleIsWatchedBy(args.Receiver, [ent]))
            return;

        // Если текущий уровень страха выше, чем тот, что мы хотим поставить,
        // то мы не должны его ставить.
        if (ent.Comp.State < source.UponComeCloser)
            TrySetFearLevel(ent.AsNullable(), source.UponComeCloser);

        StartEffects(ent, source.PlayHeartbeatSound, source.PlayBreathingSound);
        RecalculateEffectsStrength(ent.Owner, args.Range, args.CloseRange);

        SetRangeBasedShaderStrength<GrainOverlayComponent>(ent.Owner,
            args.Range,
            args.CloseRange,
            source.GrainShaderStrength,
            args.Type,
            ent.Comp);

        SetRangeBasedShaderStrength<VignetteOverlayComponent>(ent.Owner,
            args.Range,
            args.CloseRange,
            source.VignetteShaderStrength,
            args.Type,
            ent.Comp);

        AddNegativeMoodEffect(ent, MoodSourceClose);
    }

    /// <summary>
    /// Обрабатывает событие, когда сущность НЕ находится рядом с источником страха.
    /// Нужен, чтобы выключить эффекты от источника страха.
    /// </summary>
    private void OnProximityNotInRange(Entity<FearComponent> ent, ref ProximityNotInRangeTargetEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_mobState.IsAlive(ent))
            return;

        // Как только игрок отходит от источника страха он должен перестать бояться
        // Но значения шейдера от уровня страха должны продолжать действовать, что и учитывает метод
        SetShaderStrength<GrainOverlayComponent>(ent.Owner, ent.Comp, 0f);
        SetShaderStrength<VignetteOverlayComponent>(ent.Owner, ent.Comp, 0f);

        RemoveEffects(ent.Owner);
        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodSourceClose));
    }

    /// <summary>
    /// Обрабатывает событие изменения уровня страха у персонажа.
    /// </summary>
    private void OnFearStateChanged(Entity<FearComponent> ent, ref FearStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        PlayFearStateSound(ent, args.OldState);

        // Добавляем геймплейные проблемы, завязанный на уровне страха
        ManageShootingProblems(ent);
        ManageStateBasedMood(ent);

        // Проверка на то, что уровень понизился -> мы успокоились.
        // Геймплейные штуки ниже не нужно триггерить.
        if (!IsIncreasing(args.NewState, args.OldState))
            return;

        ManageJitter(ent);
        ManageAdrenaline(ent);
        TryScream(ent);
    }

    protected virtual void OnShutdown(Entity<FearComponent> ent, ref ComponentShutdown args)
    {
        _shaderStrength.TrySetAdditionalStrength<GrainOverlayComponent>(ent.Owner, 0f);
        _shaderStrength.TrySetAdditionalStrength<VignetteOverlayComponent>(ent.Owner, 0f);

        RemoveEffects(ent.Owner);

        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodFearSourceSeen));
        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodSourceClose));
    }

    protected virtual void OnRejuvenate(Entity<FearComponent> ent, ref RejuvenateEvent args)
    {
        TrySetFearLevel(ent.AsNullable(), FearState.None);

        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodFearSourceSeen));
    }

    /// <summary>
    /// Обрабатывает событие осмотра персонажа с компонентом страха.
    /// Позволяет увидеть текущий уровень страха, посмотрев на человека, в ИЦ форме.
    /// </summary>
    private void OnExamine(Entity<FearComponent> ent, ref ExaminedEvent args)
    {
        var isDead = _mobState.IsDead(ent);

        var text = isDead
            ? GetDeadExamineText(ent, ent.Comp.State)
            : GetExamineText(ent, ent.Comp.State);

        if (text == null)
            return;

        args.PushMarkup(text, -1000);
    }

    /// <summary>
    /// Пытается установить переданный уровень страха.
    /// </summary>
    public bool TrySetFearLevel(Entity<FearComponent?> ent, FearState state)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State == state)
            return false;

        var entity = (ent, ent.Comp);
        var ev = new FearStateChangedEvent(state, ent.Comp.State);

        ent.Comp.State = state;

        RaiseLocalEvent(ent, ref ev);

        SetFearBasedShaderStrength(entity);
        SetNextCalmDownTime(entity);

        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Устанавливает силу шейдеров в зависимости от текущего уровня страха.
    /// </summary>
    private void SetFearBasedShaderStrength(Entity<FearComponent> ent)
    {
        var grainStrength = ent.Comp.FearBasedGrainStrength[ent.Comp.State];
        var vignetteStrength = ent.Comp.FearBasedVignetteStrength[ent.Comp.State];

        _shaderStrength.TrySetAdditionalStrength<GrainOverlayComponent>(ent.Owner, grainStrength);
        _shaderStrength.TrySetAdditionalStrength<VignetteOverlayComponent>(ent.Owner, vignetteStrength);

        ent.Comp.CurrentFearBasedShaderStrength[nameof(GrainOverlayComponent)] = grainStrength;
        ent.Comp.CurrentFearBasedShaderStrength[nameof(VignetteOverlayComponent)] = vignetteStrength;

        Dirty(ent);
    }

    /// <summary>
    /// Устанавливает для шейдера параметры силы.
    /// Сила зависит от расстояния до источника страха и параметров самого источника.
    /// </summary>
    /// <param name="ent">Сущность, с компонентами силы шейдера</param>
    /// <param name="currentRange">Текущее расстояние сущности до источника страха</param>
    /// <param name="maxRange">Расстояние, на котором начинаются увеличиваться параметры шейдера</param>
    /// <param name="parameters">Параметры силы шейдера. Минимальное означает силу шейдера на входе в <see cref="maxRange"/>,
    /// максимальное силу при максимальном приближении к источнику страха</param>
    /// <param name="type"> <see cref="LineOfSightBlockerLevel"/> </param>
    /// <param name="fear">Компонент страха</param>
    /// <typeparam name="T">Компонент с настройками шейдера</typeparam>
    private void SetRangeBasedShaderStrength<T>(Entity<T?> ent,
        float currentRange,
        float maxRange,
        MinMaxExtended parameters,
        LineOfSightBlockerLevel type,
        FearComponent? fear = null)
        where T : IShaderStrength, IComponent
    {
        if (!Resolve(ent, ref fear))
            return;

        var strength = CalculateShaderStrength(currentRange, maxRange, parameters);

        if (type == LineOfSightBlockerLevel.Transparent)
            strength /= fear.TransparentStrengthDecreaseFactor;

        SetShaderStrength(ent, fear, strength);
    }

    /// <summary>
    /// Устанавливает силу шейдера, учитывая текущий уровень страха и его потребности в силе.
    /// </summary>
    /// <typeparam name="T">Компонент настроек, контролирующий силу шейдера</typeparam>
    private void SetShaderStrength<T>(Entity<T?> ent, FearComponent? fear, float strength)
        where T : IShaderStrength, IComponent
    {
        if (!Resolve(ent, ref fear))
            return;

        var actualStrength = GetActualStrength<T>(fear, strength);
        actualStrength /= GetDrunkModifier(ent);

        if (IsStrengthSimilar(ent, actualStrength))
            return;

        _shaderStrength.TrySetAdditionalStrength(ent, actualStrength);
    }

    /// <summary>
    /// Проверяет, является ли текущая сила шейдера близкой к текущей.
    /// </summary>
    private bool IsStrengthSimilar<T>(Entity<T?> ent, float strength)
        where T : IShaderStrength, IComponent
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        // Ранний выход, чтобы не просчитывать всякое, если текущая сила уже равна подобной.
        if (MathHelper.CloseTo(ent.Comp.AdditionalStrength, strength))
            return true;

        return false;
    }
}
