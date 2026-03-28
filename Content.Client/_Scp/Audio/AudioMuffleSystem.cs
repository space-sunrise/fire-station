using System.Linq;
using Content.Client._Scp.Audio.Components;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._Scp.Audio;

/// <summary>
/// Applies SCP-specific positional sound muffling on the client based on computed occlusion.
/// </summary>
/// <remarks>
/// This system is responsible for two separate but related tasks:
/// <list type="bullet">
/// <item><description>Deriving a coarse occlusion band used by <see cref="AudioEffectResolverSystem"/>.</description></item>
/// <item><description>Applying additional gain attenuation so heavily occluded sounds become quiet or fully silent.</description></item>
/// </list>
/// It intentionally runs after <see cref="AudioSystem"/> so it can build on top of the engine's current positional,
/// distance, and occlusion state instead of competing with it.
/// </remarks>
public sealed partial class AudioMuffleSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AudioEffectResolverSystem _resolver = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    /// Cached value of <see cref="ScpCCVars.AudioMufflingEnabled"/>.
    /// </summary>
    private bool _isClientSideEnabled;

    /// <summary>
    /// Exponential falloff coefficient used to convert occlusion into an additional gain multiplier.
    /// </summary>
    private float _occlusionGainFalloff;

    /// <summary>
    /// Occlusion level at or above which a sound is treated as fully silent for SCP muffling.
    /// </summary>
    private float _silentOcclusionThreshold;

    /// <summary>
    /// Gain multiplier below which the source is clamped to zero instead of remaining faintly audible.
    /// </summary>
    private float _minAudibleGainFactor;

    /// <summary>
    /// Occlusion threshold for entering the <see cref="AudioOcclusionBand.Muffled"/> state.
    /// </summary>
    private float _muffleEffectApplyOcclusionThreshold;

    /// <summary>
    /// Lower hysteresis threshold for leaving the <see cref="AudioOcclusionBand.Muffled"/> state.
    /// </summary>
    private float _muffleEffectClearOcclusionThreshold;

    /// <summary>
    /// Cached query used to exempt Station AI listeners from client-side muffling.
    /// </summary>
    private EntityQuery<StationAiHeldComponent> _aiQuery;

    #region CCvar events

    /// <summary>
    /// Binds muffling cvars, initializes the occlusion override, and schedules the system after engine audio updates.
    /// </summary>
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
        InitializeOcclusion();
    }

    /// <summary>
    /// Removes the custom occlusion hook installed during <see cref="Initialize"/>.
    /// </summary>
    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownOcclusion();
    }

    #endregion

    /// <summary>
    /// Re-evaluates tracked positional sounds once per frame after the engine has refreshed positional audio state.
    /// </summary>
    /// <param name="frameTime">The current frame duration in seconds.</param>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        IterateAudios();
    }

    /// <summary>
    /// Iterates tracked positional sounds on the same cadence as the engine audio update.
    /// This avoids a full scan over every audio entity every render frame.
    /// </summary>
    /// <remarks>
    /// The scan is limited to sounds tracked by <see cref="AudioEffectResolverSystem"/>, which keeps the work bounded
    /// to audio entities that have already opted into the SCP local-effects pipeline.
    /// </remarks>
    private void IterateAudios()
    {
        var hasLocalEntity = Exists(_player.LocalEntity);

        var player = hasLocalEntity
            ? _player.LocalEntity!.Value
            : EntityUid.Invalid;

        var canMuffle = _isClientSideEnabled &&
                        hasLocalEntity &&
                        !_aiQuery.HasComp(player);

        foreach (var uid in _resolver.TrackedAudio.ToArray())
        {
            if (!_resolver.TryGetTrackedAudio(uid, out var audioComp, out var localEffects))
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (_resolver.IsEffectivelyGlobal(uid, audioComp))
            {
                _resolver.SetOcclusionBand(uid, AudioOcclusionBand.Clear);
                _resolver.Reconcile(uid, audioComp, localEffects);
                continue;
            }

            if (!audioComp.Loaded || !audioComp.Started)
                continue;

            if (hasLocalEntity && audioComp.ExcludedEntity == player)
                continue;

            UpdateMuffleEffect(uid, audioComp, localEffects, canMuffle);
            ApplyOcclusionGain(audioComp, canMuffle);
            _resolver.Reconcile(uid, audioComp, localEffects);
        }
    }

    /// <summary>
    /// Updates the content-side occlusion band used by the resolver.
    /// </summary>
    /// <param name="uid">The tracked audio entity being updated.</param>
    /// <param name="audioComp">Its audio component with the latest engine occlusion value.</param>
    /// <param name="localEffects">The stored SCP-local effect state for the sound.</param>
    /// <param name="canMuffle">
    /// Whether muffling is currently allowed for the listener, taking client settings and special viewpoints into
    /// account.
    /// </param>
    private void UpdateMuffleEffect(
        EntityUid uid,
        AudioComponent audioComp,
        AudioLocalEffectsComponent localEffects,
        bool canMuffle)
    {
        var band = canMuffle
            ? GetOcclusionBand(audioComp.Occlusion, localEffects.OcclusionBand)
            : AudioOcclusionBand.Clear;

        _resolver.SetOcclusionBand(uid, band);
    }

    /// <summary>
    /// Applies additional gain attenuation derived from occlusion without ever restoring gain above the engine result.
    /// </summary>
    /// <param name="audioComp">The audio component whose gain should be attenuated further.</param>
    /// <param name="canMuffle">Whether muffling attenuation is currently permitted for the listener.</param>
    /// <remarks>
    /// The engine may already mute the source because of distance, map mismatch, nullspace, or built-in occlusion.
    /// This method therefore clamps downward only and never increases <see cref="AudioComponent.Gain"/>.
    /// </remarks>
    private void ApplyOcclusionGain(AudioComponent audioComp, bool canMuffle)
    {
        var occlusion = audioComp.Occlusion;

        float gainFactor;
        if (!canMuffle || occlusion <= 0f)
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
    /// Converts raw occlusion into a stable band used by the resolver.
    /// </summary>
    /// <param name="occlusion">The raw occlusion value currently reported for the sound.</param>
    /// <param name="currentBand">The band assigned during the previous update, used for hysteresis.</param>
    /// <returns>The new coarse occlusion band for the source.</returns>
    /// <remarks>
    /// Hysteresis prevents sources from flickering between clear and muffled when their occlusion hovers around the
    /// threshold while either the listener or the source is moving.
    /// </remarks>
    private AudioOcclusionBand GetOcclusionBand(float occlusion, AudioOcclusionBand currentBand)
    {
        if (occlusion >= _silentOcclusionThreshold)
            return AudioOcclusionBand.Silent;

        var threshold = currentBand == AudioOcclusionBand.Clear
            ? _muffleEffectApplyOcclusionThreshold
            : _muffleEffectClearOcclusionThreshold;

        return occlusion >= threshold
            ? AudioOcclusionBand.Muffled
            : AudioOcclusionBand.Clear;
    }

    /// <summary>
    /// Handles runtime toggling of the client-side audio muffling feature.
    /// </summary>
    /// <param name="enabled">Whether muffling should remain active for subsequently updated sounds.</param>
    /// <remarks>
    /// Disabling muffling immediately clears the occlusion band on every tracked sound and asks the resolver to remove
    /// any muffling-owned auxiliary that is still attached.
    /// </remarks>
    private void OnToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;

        if (enabled)
            return;

        foreach (var uid in _resolver.TrackedAudio.ToArray())
        {
            _resolver.SetOcclusionBand(uid, AudioOcclusionBand.Clear);
            _resolver.Reconcile(uid);
        }
    }
}
