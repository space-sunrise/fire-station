using System.Linq;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Systems;
using Content.Shared._Sunrise.Mood;
using Content.Shared.Mobs.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem : SharedFearSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<FearActiveSoundEffectsComponent> _activeFearEffects;

    private static readonly TimeSpan CalmDownCheckCooldown = TimeSpan.FromSeconds(1f);
    private TimeSpan _nextCalmDownCheck = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        InitializeSoundEffects();
        InitializeFears();
        InitializeGameplay();
        InitializeTraits();
        InitializeEntityEffects();

        _activeFearEffects = GetEntityQuery<FearActiveSoundEffectsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateCalmDown();
        UpdateHemophobia();
    }

    private void UpdateCalmDown()
    {
        if (_timing.CurTime < _nextCalmDownCheck)
            return;

        var query = EntityQueryEnumerator<FearComponent, MobStateComponent>();

        // Проходимся по людям с компонентом страха и уменьшаем уровень страха со временем
        while (query.MoveNext(out var uid, out var fear, out var mob))
        {
            if (!_mob.IsAlive(uid, mob))
                continue;

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

        _nextCalmDownCheck = _timing.CurTime + CalmDownCheckCooldown;
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

    protected override void OnShutdown(Entity<FearComponent> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        RemoveMoodEffects(ent);
    }

    protected override void OnRejuvenate(Entity<FearComponent> ent, ref RejuvenateEvent args)
    {
        base.OnRejuvenate(ent, ref args);

        RemoveMoodEffects(ent);
    }

    private void RemoveMoodEffects(EntityUid uid)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent(MoodSomeoneDiedOnMyEyes));
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent(MoodHemophobicSeeBlood));
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent(MoodHemophobicBleeding));
    }

    protected override void Clear()
    {
        base.Clear();

        _nextHemophobiaCheck = TimeSpan.Zero;
        _nextCalmDownCheck = TimeSpan.Zero;
    }
}
