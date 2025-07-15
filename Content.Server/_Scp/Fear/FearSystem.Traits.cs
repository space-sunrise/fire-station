using Content.Server.Administration.Systems;
using Content.Server.Speech.Components;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Components.Traits;
using Content.Shared.Administration;
using Content.Shared.Bed.Sleep;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly AdminFrozenSystem _adminFrozen = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private static readonly ProtoId<StatusEffectPrototype> JitterStatusEffectKey = "Jitter";

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

        AddStupor(ent, ent.Comp.StuporTime);
    }

    /// <summary>
    /// Добавляет эффект оцепенения на персонажа на указанное время.
    /// </summary>
    private void AddStupor(EntityUid uid, TimeSpan time)
    {
        _adminFrozen.FreezeAndMute(uid);
        RemoveComponentAfter<AdminFrozenComponent>(uid, time);
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

        _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(ent,
            FearFaintingComponent.StatusEffectKey,
            ent.Comp.Time,
            false);
    }
}
