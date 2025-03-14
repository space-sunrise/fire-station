using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Audio;

public sealed class AudioEffectsManagerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Захешированные эффекты под их прототипами пренитов. Позволяет не засрать слоты OpenAL сотней одинаковых эффектов
    /// </summary>
    private static readonly Dictionary<ProtoId<AudioPresetPrototype>, EntityUid> CachedEffects = new ();

    /// <summary>
    /// Добавляет переданный эффект к звуку
    /// </summary>
    public bool TryAddEffect(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype> preset)
    {
        if (!CachedEffects.TryGetValue(preset, out var effect) && !TryCreateEffect(preset, out effect))
            return false;

        _audio.SetAuxiliary(sound, sound, effect);
        return true;
    }

    /// <summary>
    /// Пытается создать эффект и захешировать его
    /// </summary>
    /// <param name="preset">Пресет эффектов</param>
    /// <param name="effectStuff">Получаемый эффект. Не представляет собой ничего, когда метод возвращает false</param>
    /// <returns>Возвращает успешно ли создание и хеширование эффекта</returns>
    public bool TryCreateEffect(ProtoId<AudioPresetPrototype> preset, out EntityUid effectStuff)
    {
        effectStuff = default;

        if (!_prototype.TryIndex(preset, out var prototype))
            return false;

        var effect = _audio.CreateEffect();
        var auxiliary = _audio.CreateAuxiliary();

        _audio.SetEffectPreset(effect.Entity, effect.Component, prototype);
        _audio.SetEffect(auxiliary.Entity, auxiliary.Component, effect.Entity);

        if (!Exists(auxiliary.Entity))
            return false;

        if (!CachedEffects.TryAdd(preset, auxiliary.Entity))
            return false;

        effectStuff = auxiliary.Entity;

        return true;
    }
}
