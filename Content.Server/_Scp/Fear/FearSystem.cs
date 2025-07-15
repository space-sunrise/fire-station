﻿using System.Linq;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Content.Shared._Sunrise.Mood;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem : SharedFearSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<FearActiveSoundEffectsComponent> _activeFearEffects;

    public override void Initialize()
    {
        base.Initialize();

        InitializeSoundEffects();
        InitializeFears();
        InitializeGameplay();
        InitializeTraits();

        _activeFearEffects = GetEntityQuery<FearActiveSoundEffectsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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

        UpdateHemophobia();
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

        // АХТУНГ, МИСПРЕДИКТ!!
        // Использовать только с сервера до предикта Solution
        var attempt = new FearCalmDownAttemptEvent(newFearState);
        RaiseLocalEvent(ent, attempt);

        if (attempt.Cancelled)
            return false;

        if (!TrySetFearLevel(ent.AsNullable(), newFearState))
            return false;

        return true;
    }

    protected override void OnRejuvenate(Entity<FearComponent> ent, ref RejuvenateEvent args)
    {
        base.OnRejuvenate(ent, ref args);

        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodSomeoneDiedOnMyEyes));
        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodHemophobicSeeBlood));
        RaiseLocalEvent(ent, new MoodRemoveEffectEvent(MoodHemophobicBleeding));
    }
}
