using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Audio.Components;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

public sealed partial class AudioMuffleSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly ProtoId<AudioPresetPrototype> MufflingEffectPreset = "ScpBehindWalls";

    private bool _isClientSideEnabled;
    private float _occlusionGainFalloff;
    private float _silentOcclusionThreshold;
    private float _minAudibleGainFactor;
    private float _muffleEffectApplyOcclusionThreshold;
    private float _muffleEffectClearOcclusionThreshold;

    private EntityQuery<StationAiHeldComponent> _aiQuery;
    private EntityQuery<AudioMuffledComponent> _audioMuffledQuery;

    #region CCvar events

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
        UpdatesAfter.Add(typeof(AudioSystem));

        Subs.CVar(_cfg, ScpCCVars.AudioMufflingEnabled, OnToggled, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingOcclusionGainFalloff, value => _occlusionGainFalloff = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingSilentOcclusionThreshold, value => _silentOcclusionThreshold = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingMinAudibleGainFactor, value => _minAudibleGainFactor = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingEffectApplyOcclusionThreshold, value => _muffleEffectApplyOcclusionThreshold = value, true);
        Subs.CVar(_cfg, ScpCCVars.AudioMufflingEffectClearOcclusionThreshold, value => _muffleEffectClearOcclusionThreshold = value, true);

        _aiQuery = GetEntityQuery<StationAiHeldComponent>();
        _audioMuffledQuery = GetEntityQuery<AudioMuffledComponent>();
        InitializeOcclusion();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownOcclusion();
    }

    #endregion

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_isClientSideEnabled)
            return;

        IterateAudios();
    }

    /// <summary>
    /// Iterates over active audio entities and applies content-side muffling state.
    /// </summary>
    /// <remarks>
    /// This runs as a post-pass over audio because the engine audio API does not expose a cleaner content hook for
    /// effect management, and positional data becomes valid later than component creation and playback startup.
    /// </remarks>
    private void IterateAudios()
    {
        if (!Exists(_player.LocalEntity))
            return;

        // Station AI should not have positional audio muffled away.
        if (_aiQuery.HasComp(_player.LocalEntity))
            return;

        var player = _player.LocalEntity.Value;
        var query = EntityQueryEnumerator<AudioComponent, MetaDataComponent>();
        while (query.MoveNext(out var sound, out var audioComp, out var meta))
        {
            if (TerminatingOrDeleted(sound, meta))
                continue;

            // Detached/nullspace audio must stay governed by AudioSystem's own mute logic.
            if ((meta.Flags & MetaDataFlags.Detached) != 0)
                continue;

            // Global sounds such as music should not be muffled.
            if (audioComp.Global || !audioComp.Loaded || !audioComp.Started)
                continue;

            if (audioComp.ExcludedEntity == player)
                continue;

            UpdateMuffleEffect((sound, audioComp));
            ApplyOcclusionGain(audioComp);
        }
    }

    /// <summary>
    /// Applies or removes the muffling effect preset based on the current occlusion value.
    /// </summary>
    private void UpdateMuffleEffect(Entity<AudioComponent> ent)
    {
        if (ent.Comp.Occlusion >= _silentOcclusionThreshold)
            return;

        var threshold = _audioMuffledQuery.HasComp(ent)
            ? _muffleEffectClearOcclusionThreshold
            : _muffleEffectApplyOcclusionThreshold;

        if (ent.Comp.Occlusion >= threshold)
            TryMuffleSound(ent);
        else
            TryUnMuffleSound(ent);
    }

    /// <summary>
    /// Applies additional gain attenuation derived from occlusion without ever restoring gain above the engine result.
    /// </summary>
    private void ApplyOcclusionGain(AudioComponent audioComp)
    {
        var occlusion = audioComp.Occlusion;

        float gainFactor;
        if (occlusion <= 0f)
        {
            gainFactor = 1f;
        }
        else if (occlusion >= _silentOcclusionThreshold)
        {
            gainFactor = 0f;
        }
        else
        {
            gainFactor = MathF.Exp(-occlusion * _occlusionGainFalloff);

            if (gainFactor < _minAudibleGainFactor)
                gainFactor = 0f;
        }

        var targetGain = SharedAudioSystem.VolumeToGain(audioComp.Params.Volume) * gainFactor;

        // AudioSystem may already have muted this source for distance/map/nullspace.
        // Only ever attenuate further, never restore gain above the engine's current value.
        if (audioComp.Gain > targetGain)
            audioComp.Gain = targetGain;
    }

    /// <summary>
    /// Tries to apply the muffling effect to a sound.
    /// </summary>
    /// <param name="ent">The audio entity to modify.</param>
    /// <returns>True if the effect was applied; otherwise false.</returns>
    public bool TryMuffleSound(Entity<AudioComponent> ent)
    {
        if (_audioMuffledQuery.HasComp(ent))
            return false;

        // Store prior effect state in a marker component so it can be restored later.
        var muffledComponent = AddComp<AudioMuffledComponent>(ent);
        muffledComponent.CachedVolume = ent.Comp.Volume;

        if (_effectsManager.TryGetEffect(ent, out var preset))
            muffledComponent.CachedPreset = preset;

        // Clear incompatible effects, such as echo, before applying the muffling preset.
        _effectsManager.RemoveAllEffects(ent);

        _effectsManager.TryAddEffect(ent, MufflingEffectPreset);

        return true;
    }

    /// <summary>
    /// Tries to remove the muffling effect from a sound.
    /// </summary>
    /// <param name="ent">The audio entity to modify.</param>
    /// <param name="muffledComponent">The cached muffling marker component.</param>
    /// <returns>True if the effect was removed; otherwise false.</returns>
    public bool TryUnMuffleSound(Entity<AudioComponent> ent, AudioMuffledComponent? muffledComponent = null)
    {
        if (!_audioMuffledQuery.Resolve(ent.Owner, ref muffledComponent, false))
            return false;

        _effectsManager.TryRemoveEffect(ent, MufflingEffectPreset);

        if (muffledComponent.CachedPreset != null)
            _effectsManager.TryAddEffect(ent, muffledComponent.CachedPreset.Value);

        RemComp<AudioMuffledComponent>(ent);

        return true;
    }

    /// <summary>
    /// Handles runtime toggling of the client-side audio muffling feature.
    /// </summary>
    private void OnToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;

        if (!enabled)
        {
            RevertChanges();
        }
    }

    /// <summary>
    /// Restores all sounds that still have the muffling marker component.
    /// Used when the player disables the client-side muffling feature.
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
