using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Audio.Components;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

public sealed class AudioMuffleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly ProtoId<AudioPresetPrototype> MufflingEffectPreset = "ScpBehindWalls";

    private const float ReducedVolume = -20f;

    private bool _isClientSideEnabled;
    // Отвечает за выбор цикла для итерации звуков.
    // При true будет использована итерация каждый фрейм, что гораздо больше, чем стандартный Update
    // Но это может позволить избежать проблем со звуками, которые успеют издать пук между тиками
    // При true будет использовать FrameUpdate. При false стандартный завязанный на тиках Update
    private bool _useHighFrequencyUpdate;

    #region CCvar events

    public override void Initialize()
    {
        base.Initialize();

        _isClientSideEnabled = _cfg.GetCVar(ScpCCVars.AudioMufflingEnabled);

        _cfg.OnValueChanged(ScpCCVars.AudioMufflingEnabled, OnToggled);
        _cfg.OnValueChanged(ScpCCVars.AudioMufflingHighFrequencyUpdate, b => _useHighFrequencyUpdate = b);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.AudioMufflingEnabled, OnToggled);
        _cfg.UnsubValueChanged(ScpCCVars.AudioMufflingHighFrequencyUpdate, b => _useHighFrequencyUpdate = b);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_isClientSideEnabled)
            return;

        if (_useHighFrequencyUpdate)
            return;

        IterateAudios();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_isClientSideEnabled)
            return;

        if (!_useHighFrequencyUpdate)
            return;

        IterateAudios();
    }

    /// <summary>
    /// Производит итерацию по всем звукам и расставляет эффект заглушения, в зависимости от позиции звука
    /// </summary>
    /// <remarks>
    /// Используется именно итерация, потому что API системы звука отвратителен в этом плане и не предназначен для работы с эффектами
    /// А так же звук начинает иметь позицию намного позже, чем появляются компонент звука и начинается проигрывание.
    /// Единственный способ реализовать задуманное в условиях контента это итерация каждый тик.
    /// </remarks>
    private void IterateAudios()
    {
        if (!Exists(_player.LocalEntity))
            return;

        var player = _player.LocalEntity.Value;
        var query = AllEntityQuery<AudioComponent>();

        while (query.MoveNext(out var sound, out var audioComp))
        {
            if (TerminatingOrDeleted(sound))
                continue;

            // Глобальные звуки(музыка и т.д) не должны поддаваться заглушению
            if (audioComp.Global)
                continue;

            var lineOfSight = _proximity.GetLightOfSightBlockerLevel(sound, player);

            // В чем прикол этой конструкции снизу:
            // Если что-то находится за каким-то объектом и его НЕ видно через стекла -> 1ый случай.
            // Примеры первого случая - что-то за стеной.
            // Если что-то находится за объектом и его видно через стекло -> 2ой случай.
            // Примеры второго случая: Что-то за стеклом или стеклянным шлюзом
            // Если ничего из этого, значит объект в прямой зоне видимости игрока
            // И никаких эффектов заглушения быть не должно. Поэтому мы снимаем эффекты
            switch (lineOfSight)
            {
                case LineOfSightBlockerLevel.Solid:
                    TryMuffleSound((sound, audioComp));
                    break;
                case LineOfSightBlockerLevel.Transparent:
                    TryMuffleSound((sound, audioComp), false);
                    break;
                case LineOfSightBlockerLevel.None:
                    TryUnMuffleSound((sound, audioComp));
                    break;
                default:
                    TryUnMuffleSound((sound, audioComp));
                    break;
            }
        }
    }

    /// <summary>
    /// Пробует добавить эффект заглушения звука
    /// </summary>
    /// <param name="ent">Звук, на который будет добавлен эффект</param>
    /// <param name="decreaseVolume">Будет ли понижаться громкость?</param>
    /// <returns>Получилось ли добавить эффект?</returns>
    public bool TryMuffleSound(Entity<AudioComponent> ent, bool decreaseVolume = true)
    {
        if (AudioEffectsManagerSystem.HasEffect(ent, MufflingEffectPreset))
            return false;

        if (HasComp<AudioMuffledComponent>(ent))
            return false;

        // Добавляем компонент-маркер, что звук заглушен
        // В нем будут храниться хешированные прошлые параметры
        var muffledComponent = EnsureComp<AudioMuffledComponent>(ent);
        muffledComponent.CachedVolume = ent.Comp.Volume;

        if (_effectsManager.TryGetEffect(ent, out var preset))
            muffledComponent.CachedPreset = preset;

        // Очищение лишних эффектов(например эхо)
        _effectsManager.RemoveAllEffects(ent);

        _effectsManager.TryAddEffect(ent, MufflingEffectPreset);

        if (decreaseVolume)
            _audio.SetVolume(ent, ent.Comp.Volume + ReducedVolume, ent);

        return true;
    }

    /// <summary>
    /// Пытается снять эффект заглушения звука
    /// </summary>
    /// <param name="ent">Звук, с которого будет снят эффект</param>
    /// <param name="muffledComponent">Компонент заглушенного звука</param>
    /// <returns></returns>
    public bool TryUnMuffleSound(Entity<AudioComponent> ent, AudioMuffledComponent? muffledComponent = null)
    {
        if (!AudioEffectsManagerSystem.HasEffect(ent, MufflingEffectPreset))
            return false;

        if (!Resolve(ent.Owner, ref muffledComponent))
            return false;

        _effectsManager.TryRemoveEffect(ent, MufflingEffectPreset);

        if (muffledComponent.CachedPreset != null)
            _effectsManager.TryAddEffect(ent, muffledComponent.CachedPreset.Value);

        _audio.SetVolume(ent, muffledComponent.CachedVolume, ent);

        RemComp<AudioMuffledComponent>(ent);

        return true;
    }

    private void OnToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;

        if (!enabled)
            RevertChanges();
    }

    /// <summary>
    /// Возвращает всем звукам предыдущие незаглушенные параметры.
    /// Используется при отключении игроком настройки, отвечающей за механику заглушения звуков.
    /// </summary>
    private void RevertChanges()
    {
        var query = AllEntityQuery<AudioMuffledComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var muffled, out var audio))
        {
            TryUnMuffleSound((uid, audio), muffled);
        }
    }
}
