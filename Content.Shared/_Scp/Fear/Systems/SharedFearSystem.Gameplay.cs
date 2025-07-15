using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Weapons.Ranged;
using Content.Shared._Sunrise.Mood;
using Content.Shared.Administration;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    [Dependency] private readonly StatusEffectsSystem _effects = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    private const float BaseJitteringAmplitude = 1f;
    private const float BaseJitteringFrequency = 4f;

    private const string AdrenalineEffectKey = "Adrenaline";

    private static readonly Dictionary<FearState, string> FearMoodStates = new()
    {
        { FearState.Anxiety, "FearStateAnxiety" },
        { FearState.Fear, "FearStateFear" },
        { FearState.Terror, "FearStateTerror" },
    };

    /// <summary>
    /// Регулирует проблемы со стрельбой при увеличении страха
    /// </summary>
    private void ManageShootingProblems(Entity<FearComponent> ent)
    {
        if (ent.Comp.State == FearState.None)
        {
            RemComp<DispersingShotSourceComponent>(ent);
            return;
        }

        var component = EnsureComp<DispersingShotSourceComponent>(ent);
        var modifier = ent.Comp.FearBasedSpreadAngleModifier[ent.Comp.State];

        component.AngleIncreaseMultiplier = modifier;
        component.MaxAngleMultiplier = modifier;

        Dirty(ent, component);
    }

    private void ManageJitter(Entity<FearComponent> ent)
    {
        // Компонент, выдающийся при ступоре
        if (HasComp<AdminFrozenComponent>(ent))
            return;

        // Компонент, выдающийся при обмороке
        if (HasComp<ForcedSleepingComponent>(ent))
            return;

        // Значения будут коррелировать с текущем уровнем страха
        var genericModifier = GetGenericFearBasedModifier(ent.Comp.State);

        var time = ent.Comp.BaseJitterTime * genericModifier;
        var amplitude = BaseJitteringAmplitude * genericModifier;
        var frequency = BaseJitteringFrequency * genericModifier;

        _jittering.DoJitter(ent, TimeSpan.FromSeconds(time), false, amplitude, frequency);
    }

    private void ManageAdrenaline(Entity<FearComponent> ent)
    {
        var modifier = GetGenericFearBasedModifier(ent.Comp.State);
        var time = TimeSpan.FromSeconds(ent.Comp.AdrenalineBaseTime * modifier);

        _effects.TryAddStatusEffect<IgnoreSlowOnDamageComponent>(ent, AdrenalineEffectKey, time, true);
    }

    private void ManageStateBasedMood(Entity<FearComponent> ent)
    {
        if (ent.Comp.State == FearState.None)
            WipeMood(ent);

        if (!FearMoodStates.TryGetValue(ent.Comp.State, out var moodEffect))
            return;

        RaiseLocalEvent(ent, new MoodEffectEvent(moodEffect));
    }

    private void WipeMood(EntityUid uid)
    {
        foreach (var effect in FearMoodStates.Values)
        {
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent(effect));
        }
    }

    protected virtual void TryScream(Entity<FearComponent> ent) {}
}
