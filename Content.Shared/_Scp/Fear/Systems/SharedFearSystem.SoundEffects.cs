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
        if (HasComp<FearActiveSoundEffectsComponent>(uid))
            return;

        var effects = EnsureComp<FearActiveSoundEffectsComponent>(uid);
        effects.PlayHeartbeatSound = playHeartbeatSound;
        effects.PlayBreathingSound = playBreathingSound;

        Dirty(uid, effects);

        StartBreathing((uid, effects));
        StartHeartBeat((uid, effects));
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

        ent.Comp.AdditionalVolume = volume;
        ent.Comp.Pitch = currentPitch;
        ent.Comp.NextHeartbeatCooldown = TimeSpan.FromSeconds(cooldown);

        Dirty(ent);
    }

    /// <summary>
    /// Убирает все звуковые эффекты.
    /// </summary>
    private void RemoveEffects(EntityUid uid)
    {
        RemComp<FearActiveSoundEffectsComponent>(uid);
    }
}
