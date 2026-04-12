using Content.Shared._Scp.Fear.Components;

namespace Content.Shared._Scp.Fear.Systems;

// TODO: Рефактор системы сердцебиения, чтобы оперировать сердцебиение там, а не тут.
public abstract partial class SharedFearSystem
{
    public const float HeartBeatMinimumCooldown = 2f;
    public const float HeartBeatMaximumCooldown = 0.3f;

    public const float HeartBeatMinimumPitch = 1f;
    public const float HeartBeatMaximumPitch = 0.65f;

    public const float MinimumAdditionalVolume = 5f;
    public const float MaximumAdditionalVolume = 16f;

    protected virtual void StartBreathing(Entity<FearActiveSoundEffectsComponent> ent) {}
    protected virtual void StopBreathing(Entity<FearActiveSoundEffectsComponent> ent) {}

    protected virtual void StartHeartBeat(Entity<FearActiveSoundEffectsComponent> ent) {}

    /// <summary>
    /// Проигрывает специфический звук в зависимости от установленного уровня страха.
    /// Для повышения и понижения уровня звуки разные.
    /// </summary>
    protected virtual void PlayFearStateSound(Entity<FearComponent> ent, FearState oldState) {}

    /// <summary>
    /// Запускает звуковые эффекты, связанные со страхом.
    /// </summary>
    /// <param name="uid">Сущность, для которой будет запущены эффекты</param>
    /// <param name="playHeartbeatSound">Проигрывать звук сердцебиения?</param>
    /// <param name="playBreathingSound">Проигрывать звук дыхания?</param>
    private void StartEffects(EntityUid uid, bool playHeartbeatSound, bool playBreathingSound)
    {
        var existed = TryComp<FearActiveSoundEffectsComponent>(uid, out var effects);
        effects ??= EnsureComp<FearActiveSoundEffectsComponent>(uid);

        var heartbeatChanged = effects.PlayHeartbeatSound != playHeartbeatSound;
        var breathingChanged = effects.PlayBreathingSound != playBreathingSound;

        effects.PlayHeartbeatSound = playHeartbeatSound;
        effects.PlayBreathingSound = playBreathingSound;

        if (!existed || heartbeatChanged || breathingChanged)
        {
            DirtyFields(uid,
                effects,
                null,
                nameof(FearActiveSoundEffectsComponent.PlayHeartbeatSound),
                nameof(FearActiveSoundEffectsComponent.PlayBreathingSound));
        }

        if (!existed || (heartbeatChanged && playHeartbeatSound))
            StartHeartBeat((uid, effects));

        if (!existed || (breathingChanged && playBreathingSound))
            StartBreathing((uid, effects));

        if (existed && breathingChanged && !playBreathingSound)
            StopBreathing((uid, effects));
    }

    /// <summary>
    /// Пересчитывает и актуализирует параметры звуковых эффект в зависимости от расстояния до источника страха.
    /// </summary>
    private void RecalculateEffectsStrength(Entity<FearActiveSoundEffectsComponent?> ent, float currentRange, float maxRange)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var volume = CalculateStrength(currentRange, maxRange, MinimumAdditionalVolume, MaximumAdditionalVolume);

        var cooldown = CalculateStrength(currentRange, maxRange, HeartBeatMinimumCooldown, HeartBeatMaximumCooldown);
        var currentPitch = CalculateStrength(currentRange, maxRange, HeartBeatMinimumPitch, HeartBeatMaximumPitch);
        var currentCooldown = TimeSpan.FromSeconds(cooldown);

        if (MathF.Abs(ent.Comp.AdditionalVolume - volume) < 0.05f
            && MathF.Abs(ent.Comp.Pitch - currentPitch) < 0.01f
            && Math.Abs((ent.Comp.NextHeartbeatCooldown - currentCooldown).TotalMilliseconds) < 25)
        {
            return;
        }

        ent.Comp.AdditionalVolume = volume;
        ent.Comp.Pitch = currentPitch;
        ent.Comp.NextHeartbeatCooldown = currentCooldown;

        DirtyFields(ent,
            ent.Comp,
            null,
            nameof(FearActiveSoundEffectsComponent.AdditionalVolume),
            nameof(FearActiveSoundEffectsComponent.Pitch),
            nameof(FearActiveSoundEffectsComponent.NextHeartbeatCooldown));
    }

    /// <summary>
    /// Убирает все звуковые эффекты.
    /// </summary>
    private void RemoveSoundEffects(EntityUid uid)
    {
        RemComp<FearActiveSoundEffectsComponent>(uid);
    }
}
