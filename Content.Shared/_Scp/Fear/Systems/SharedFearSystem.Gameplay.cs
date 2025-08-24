using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Components.Traits;
using Content.Shared._Scp.Weapons.Ranged;
using Content.Shared._Sunrise.Mood;
using Content.Shared.Drunk;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    [Dependency] private readonly StatusEffectsSystem _effects = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    private const float BaseJitteringAmplitude = 1f;
    private const float BaseJitteringFrequency = 4f;

    private const float MinimumAlcoholModifier = 1f;
    private const float MaximumAlcoholModifier = 4f;

    private static readonly EntProtoId DrunkStatusEffect = "StatusEffectDrunk";
    private static readonly EntProtoId AdrenalineStatusEffect = "StatusEffectFearAdrenaline";

    private static readonly Dictionary<FearState, string> FearMoodStates = new()
    {
        { FearState.Anxiety, "FearStateAnxiety" },
        { FearState.Fear, "FearStateFear" },
        { FearState.Terror, "FearStateTerror" },
    };

    private EntityQuery<DrunkStatusEffectComponent> _drunkQuery;

    private void InitializeGameplay()
    {
        _drunkQuery = GetEntityQuery<DrunkStatusEffectComponent>();
    }

    /// <summary>
    /// Регулирует проблемы со стрельбой при увеличении страха
    /// </summary>
    private void ManageShootingProblems(Entity<FearComponent> ent)
    {
        if (ent.Comp.State == FearState.None)
        {
            SetSpreadParameters(ent, 1f, 1f); // Убирает модификаторы, скручивая их до 1
            return;
        }

        var modifier = ent.Comp.FearBasedSpreadAngleModifier[ent.Comp.State];
        SetSpreadParameters(ent, modifier, modifier);
    }

    private void SetSpreadParameters(EntityUid uid, float angleIncrease, float maxAngle)
    {
        var component = EnsureComp<DispersingShotSourceComponent>(uid);
        component.AngleIncreaseMultiplier = angleIncrease;
        component.MaxAngleMultiplier = maxAngle;

        Dirty(uid, component);
    }

    /// <summary>
    /// Заставляет сущность трястись от страха.
    /// Параметры тряски зависят от уровня страха
    /// </summary>
    private void ManageJitter(Entity<FearComponent> ent)
    {
        // При ступоре и обмороке персонаж не должен трястись
        if (_effects.HasStatusEffect(ent, FearStuporComponent.StatusEffect))
            return;

        if (_effects.HasStatusEffect(ent, FearFaintingComponent.StatusEffect))
            return;

        // Значения будут коррелировать с текущем уровнем страха
        var genericModifier = GetGenericFearBasedModifier(ent.Comp.State);
        var alcoholModifier = GetDrunkModifier(ent);

        var time = ent.Comp.BaseJitterTime * genericModifier / alcoholModifier;
        var amplitude = BaseJitteringAmplitude * genericModifier / alcoholModifier;
        var frequency = BaseJitteringFrequency * genericModifier / alcoholModifier;

        _jittering.DoJitter(ent, TimeSpan.FromSeconds(time), false, amplitude, frequency);
    }

    /// <summary>
    /// Вводит адреналин в кровь сущности, количество зависит от уровня страха.
    /// </summary>
    private void ManageAdrenaline(Entity<FearComponent> ent)
    {
        var modifier = GetGenericFearBasedModifier(ent.Comp.State);
        var time = TimeSpan.FromSeconds(ent.Comp.AdrenalineBaseTime * modifier);

        _effects.TryAddStatusEffectDuration(ent, AdrenalineStatusEffect, time);
    }

    /// <summary>
    /// Выставляет модификаторы настроения, зависящие от уровня страха
    /// </summary>
    private void ManageStateBasedMood(Entity<FearComponent> ent)
    {
        if (ent.Comp.State == FearState.None)
            WipeMood(ent);

        if (!FearMoodStates.TryGetValue(ent.Comp.State, out var moodEffect))
            return;

        AddNegativeMoodEffect(ent, moodEffect);
    }

    /// <summary>
    /// Убирает все стандартные модификаторы настроения, зависящие от уровня страха.
    /// </summary>
    /// <param name="uid"></param>
    private void WipeMood(EntityUid uid)
    {
        foreach (var effect in FearMoodStates.Values)
        {
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent(effect));
        }
    }

    /// <summary>
    /// Вызывает эффект негативного влияния на настроение.
    /// Сила эффекта зависит от уровня алкоголя в крови сущности, алкоголь делает негативные эффекты слабее
    /// </summary>
    protected void AddNegativeMoodEffect(EntityUid uid, string effect)
    {
        var drunkModifier = Math.Clamp(1f / GetDrunkModifier(uid), 0f, 1f);
        RaiseLocalEvent(uid, new MoodEffectEvent(effect, drunkModifier));
    }

    /// <summary>
    /// Получает модификатор, зависящий от силы алкоголя в крови сущности
    /// </summary>
    private float GetDrunkModifier(EntityUid uid)
    {
        if (!_effects.TryGetStatusEffect(uid, DrunkStatusEffect, out var drunkEffect))
            return 1f;

        if (!_drunkQuery.TryComp(drunkEffect, out var drunk))
            return 1f;

        var normalized = Math.Clamp(drunk.CurrentBoozePower / 50f, MinimumAlcoholModifier, MaximumAlcoholModifier);

        return normalized;
    }

    protected virtual void TryScream(Entity<FearComponent> ent) {}
}
