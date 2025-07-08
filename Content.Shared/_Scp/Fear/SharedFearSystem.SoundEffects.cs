namespace Content.Shared._Scp.Fear;

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

    private void StartEffects(EntityUid uid)
    {
        if (HasComp<FearActiveSoundEffectsComponent>(uid))
            return;

        var effects = EnsureComp<FearActiveSoundEffectsComponent>(uid);
        StartBreathing((uid, effects));
        StartHeartBeat((uid, effects));
    }

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

    private void RemoveEffects(EntityUid uid)
    {
        RemComp<FearActiveSoundEffectsComponent>(uid);
    }
}
