using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Weapons.Ranged;
using Content.Shared.Damage.Components;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    [Dependency] private readonly StatusEffectsSystem _effects = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    private const float BaseJitteringAmplitude = 3f;
    private const float BaseJitteringFrequency = 4f;

    private const string AdrenalineEffectKey = "Adrenaline";

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

    protected virtual void TryScream(Entity<FearComponent> ent) {}
}
