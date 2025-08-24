using Content.Server.Speech.Components;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components.Traits;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private void InitializeTraits()
    {
        SubscribeLocalEvent<FearStuporComponent, FearStateChangedEvent>(OnStuporFearStateChanged);
        SubscribeLocalEvent<FearStutteringComponent, FearStateChangedEvent>(OnStutteringFearStateChanged);
        SubscribeLocalEvent<FearFaintingComponent, FearStateChangedEvent>(OnFaintingFearStateChanged);
    }

    /// <summary>
    /// Обрабатывает событие изменения уровня страха.
    /// С шансом вызывает оцепенение у персонажа с трейтом оцепенения.
    /// </summary>
    private void OnStuporFearStateChanged(Entity<FearStuporComponent> ent, ref FearStateChangedEvent args)
    {
        // Если старый стейт больше, значит персонаж успокоился
        // От этого не нужно впадать в ступор
        if (!IsIncreasing(args.NewState, args.OldState))
            return;

        if (args.NewState < ent.Comp.RequiredState)
            return;

        var normalizedChance = PercentToNormalized(ent.Comp.Chance);
        if (!_random.Prob(normalizedChance))
            return;

        _statusEffects.TryAddStatusEffectDuration(ent, FearStuporComponent.StatusEffect, ent.Comp.StuporTime);
    }

    private void OnStutteringFearStateChanged(Entity<FearStutteringComponent> ent, ref FearStateChangedEvent args)
    {
        if (args.NewState == FearState.None)
        {
            RemComp<StutteringAccentComponent>(ent);
            return;
        }

        var stuttering = EnsureComp<StutteringAccentComponent>(ent);
        var modifier = GetGenericFearBasedModifier(args.NewState, 1);

        stuttering.CutRandomProb *= modifier;
        stuttering.FourRandomProb *= modifier;
        stuttering.ThreeRandomProb *= modifier;
    }

    private void OnFaintingFearStateChanged(Entity<FearFaintingComponent> ent, ref FearStateChangedEvent args)
    {
        // Проверяем, что стейт не увеличивается
        if (!IsIncreasing(args.NewState, args.OldState))
            return;

        if (args.NewState < ent.Comp.RequiredState)
            return;

        var percentNormalized = PercentToNormalized(ent.Comp.Chance);
        if (!_random.Prob(percentNormalized))
            return;

        _statusEffects.TryAddStatusEffectDuration(ent, FearFaintingComponent.StatusEffect, ent.Comp.Time);
    }
}
