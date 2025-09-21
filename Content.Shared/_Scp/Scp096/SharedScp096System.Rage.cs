using Content.Shared.Audio;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    /// <summary>
    /// Вызывается при окончании времени на поиск и уничтожение целей.
    /// Убирает все цели из списка целей.
    /// </summary>
    private void OnRageTimeExceeded(Entity<Scp096Component> ent)
    {
        RemoveAllTargets(ent);
    }

    /// <summary>
    /// Умиротворяет скромника.
    /// Происходит после того, как все цели были убиты и разорваны.
    /// </summary>
    private void Pacify(Entity<Scp096Component> ent)
    {
        ent.Comp.InRageMode = false;
        ent.Comp.RageStartTime = null;
        Dirty(ent);

        _ambientSound.SetSound(ent, ent.Comp.CrySound);
        _ambientSound.SetRange(ent, 4f);
        _ambientSound.SetVolume(ent, -14f);
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, ent.Comp.PacifiedTime);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(false));
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        RefreshSpeedModifiers(ent);
    }

    /// <summary>
    /// Переводит скромника в состояние ярости.
    /// </summary>
    private void MakeAngry(Entity<Scp096Component> ent)
    {
        ent.Comp.InRageMode = true;
        ent.Comp.RageStartTime = _timing.CurTime;
        Dirty(ent);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(true));
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        _ambientSound.SetSound(ent, ent.Comp.RageSound);
        _ambientSound.SetRange(ent, 20f);
        _ambientSound.SetVolume(ent, 10f);

        RefreshSpeedModifiers(ent);
    }
}
