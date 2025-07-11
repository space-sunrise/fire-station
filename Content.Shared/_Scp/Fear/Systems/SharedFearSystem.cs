using System.Linq;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Shaders;
using Content.Shared._Scp.Shaders.Grain;
using Content.Shared._Scp.Shaders.Highlighting;
using Content.Shared._Scp.Shaders.Vignette;
using Content.Shared._Scp.Watching;
using Content.Shared.Mobs.Systems;
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

    private EntityQuery<FearActiveSoundEffectsComponent> _activeFearEffects;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, EntityLookedAtEvent>(OnEntityLookedAt);

        SubscribeLocalEvent<FearComponent, ProximityInRangeTargetEvent>(OnProximityInRange);
        SubscribeLocalEvent<FearComponent, ProximityNotInRangeTargetEvent>(OnProximityNotInRange);

        _activeFearEffects = GetEntityQuery<FearActiveSoundEffectsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FearComponent>();

        // Проходимся по людям с компонентом страха и уменьшаем уровень страха со временем
        while (query.MoveNext(out var uid, out var fear))
        {
            if (fear.State == FearState.None)
                continue;

            if (fear.NextTimeDecreaseFearLevel > _timing.CurTime)
                continue;

            var entity = (uid, fear);

            // Если по какой-то причине не получилось успокоиться, то ждем снова
            // Это нужно, чтобы игрок только что отойдя от источника страха не успокоился моментально
            if (!TryCalmDown(entity))
                SetNextCalmDownTime(entity);
        }
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

        if (!TryComp<FearSourceComponent>(args.Target, out var source))
            return;

        // Если текущий уровень страха выше, чем тот, что мы хотим поставить,
        // то мы не должны его ставить.
        if (ent.Comp.State >= source.UponSeenState)
            return;

        if (!TrySetFearLevel(ent.AsNullable(), source.UponSeenState))
            return;

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

        if (!TryComp<FearSourceComponent>(args.Receiver, out var source))
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
    }

    /// <summary>
    /// Обрабатывает событие, когда сущность НЕ находится рядом с источником страха.
    /// Нужен, чтобы выключить эффекты от источника страха.
    /// </summary>
    private void OnProximityNotInRange(Entity<FearComponent> ent, ref ProximityNotInRangeTargetEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Как только игрок отходит от источника страха он должен перестать бояться
        // Но значения шейдера от уровня страха должны продолжать действовать, что и учитывает метод
        SetShaderStrength<GrainOverlayComponent>(ent.Owner, ent.Comp, 0f);
        SetShaderStrength<VignetteOverlayComponent>(ent.Owner, ent.Comp, 0f);

        RemoveEffects(ent.Owner);
    }

    /// <summary>
    /// Пытается успокоить сущность, испытывающую страх.
    /// Понижает уровень страха на 1, пока не успокоит полностью.
    /// </summary>
    public bool TryCalmDown(Entity<FearComponent> ent)
    {
        // Немного костыль, но это означает, что мы прямо сейчас испытываем какие-то приколы со страхом
        // И пугаемся чего-то в данный момент. Значит мы не должны успокаиваться.
        if (_activeFearEffects.HasComp(ent))
            return false;

        var visibleFearSources = _watching.GetAllVisibleTo<FearSourceComponent>(ent.Owner, ent.Comp.SeenBlockerLevel);

        // Проверка на то, что мы в данный момент не смотрим на какую-то страшную сущность.
        // Нельзя успокоиться, когда мы смотрим на источник страха.
        if (visibleFearSources.Any())
            return false;

        var newFearState = GetDecreasedLevel(ent.Comp.State);

        if (!TrySetFearLevel(ent.AsNullable(), newFearState))
            return false;

        return true;
    }

    /// <summary>
    /// Пытается установить переданный уровень страха.
    /// </summary>
    public bool TrySetFearLevel(Entity<FearComponent?> ent, FearState state)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State == state)
            return true;

        var entity = (ent, ent.Comp);

        PlayFearStateSound(entity, state);

        ent.Comp.State = state;

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
        _shaderStrength.TrySetAdditionalStrength(ent, actualStrength);
    }
}
