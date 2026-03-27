using System.Linq;
using Content.Client._Scp.Audio.Components;
using Content.Shared._Scp.Audio;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

/// <summary>
/// Owns the final auxiliary slot for client-side SCP audio effects.
/// Echo and muffling only write desired state; this resolver applies the winning preset.
/// </summary>
/// <remarks>
/// The resolver is the single authority for SCP-specific local audio routing on the client.
/// <para>
/// Echo logic writes <see cref="AudioLocalEffectsComponent.DesiredEchoPreset"/>, while muffling logic writes
/// <see cref="AudioLocalEffectsComponent.OcclusionBand"/>. This system combines both inputs, preserves any auxiliary
/// that existed before SCP logic touched the source, and makes the minimal routing change needed to converge the
/// actual <see cref="AudioComponent.Auxiliary"/> state.
/// </para>
/// <para>
/// Keeping this merge in one place prevents echo and muffling from fighting over the same OpenAL auxiliary slot.
/// </para>
/// </remarks>
public sealed class AudioEffectResolverSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    /// Preset used when an audible positional source is considered occluded enough to be "behind walls".
    /// </summary>
    private static readonly ProtoId<AudioPresetPrototype> MufflingEffectPreset = "ScpBehindWalls";

    /// <summary>
    /// Standard environmental echo preset used when SCP echo is enabled.
    /// </summary>
    private static readonly ProtoId<AudioPresetPrototype> StandardEchoEffectPreset = "Bathroom";

    /// <summary>
    /// Stronger environmental echo preset used when the client requests the aggressive echo variant.
    /// </summary>
    private static readonly ProtoId<AudioPresetPrototype> StrongEchoEffectPreset = "SewerPipe";

    /// <summary>
    /// Cached value of <see cref="ScpCCVars.EchoEnabled"/> used when initializing newly tracked sounds.
    /// </summary>
    private bool _isEchoEnabled;

    /// <summary>
    /// Cached value of <see cref="ScpCCVars.EchoStrongPresetPreferred"/>.
    /// </summary>
    private bool _strongEchoPresetPreferred;

    /// <summary>
    /// Set of audio entities currently managed by the SCP local audio pipeline.
    /// </summary>
    private readonly HashSet<EntityUid> _trackedAudio = [];

    /// <summary>
    /// Query cache for resolving audio components during reconciliation.
    /// </summary>
    private EntityQuery<AudioComponent> _audioQuery;

    /// <summary>
    /// Query cache for resolving the SCP-local state associated with each tracked sound.
    /// </summary>
    private EntityQuery<AudioLocalEffectsComponent> _localEffectsQuery;

    /// <summary>
    /// Query cache used to infer whether a source behaves as global by virtue of being spawned in nullspace.
    /// </summary>
    private EntityQuery<TransformComponent> _transformQuery;

    /// <summary>
    /// Read-only view of all audio entities currently tracked by the resolver.
    /// </summary>
    public IReadOnlyCollection<EntityUid> TrackedAudio => _trackedAudio;

    /// <summary>
    /// Starts tracking newly created audio entities and subscribes to echo-related client cvars.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);

        Subs.CVar(_cfg, ScpCCVars.EchoEnabled, value => _isEchoEnabled = value, true);
        Subs.CVar(_cfg, ScpCCVars.EchoStrongPresetPreferred, value => _strongEchoPresetPreferred = value, true);

        _audioQuery = GetEntityQuery<AudioComponent>();
        _localEffectsQuery = GetEntityQuery<AudioLocalEffectsComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Resolves the audio and local-effects state for a tracked entity.
    /// </summary>
    /// <param name="uid">The audio entity to inspect.</param>
    /// <param name="audio">Receives the resolved audio component when the method succeeds.</param>
    /// <param name="localEffects">Receives the SCP-local state component when the method succeeds.</param>
    /// <returns>
    /// <see langword="true"/> if the entity is tracked and both required components are still valid;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Invalid or deleted entities are automatically removed from the tracked set to keep later scans cheap.
    /// </remarks>
    public bool TryGetTrackedAudio(EntityUid uid, out AudioComponent audio, out AudioLocalEffectsComponent localEffects)
    {
        audio = default!;
        localEffects = default!;

        if (!_trackedAudio.Contains(uid))
            return false;

        if (TerminatingOrDeleted(uid) ||
            !_audioQuery.TryComp(uid, out var resolvedAudio) ||
            !_localEffectsQuery.TryComp(uid, out var resolvedLocalEffects))
        {
            Untrack(uid);
            return false;
        }

        audio = resolvedAudio;
        localEffects = resolvedLocalEffects;
        return true;
    }

    /// <summary>
    /// Stores the echo preset currently requested for a tracked sound.
    /// </summary>
    /// <param name="uid">The audio entity whose desired echo preset should be updated.</param>
    /// <param name="preset">
    /// The requested echo preset, or <see langword="null"/> when echo should be disabled for this source.
    /// </param>
    public void SetDesiredEchoPreset(EntityUid uid, ProtoId<AudioPresetPrototype>? preset)
    {
        if (!_localEffectsQuery.TryComp(uid, out var localEffects))
            return;

        localEffects.DesiredEchoPreset = preset;
    }

    /// <summary>
    /// Stores the current occlusion band for a tracked sound.
    /// </summary>
    /// <param name="uid">The audio entity whose occlusion band should be updated.</param>
    /// <param name="band">The coarse occlusion state computed by the muffling system.</param>
    public void SetOcclusionBand(EntityUid uid, AudioOcclusionBand band)
    {
        if (!_localEffectsQuery.TryComp(uid, out var localEffects))
            return;

        localEffects.OcclusionBand = band;
    }

    /// <summary>
    /// Ensures that an audio entity is tracked by the resolver and that its original auxiliary state is preserved.
    /// </summary>
    /// <param name="ent">The audio entity being registered for SCP local effects.</param>
    /// <remarks>
    /// The original auxiliary is only captured when the resolver is not already responsible for the currently applied
    /// preset. This avoids treating an SCP-owned auxiliary as the new baseline.
    /// </remarks>
    public void EnsureTracked(Entity<AudioComponent> ent)
    {
        var localEffects = EnsureComp<AudioLocalEffectsComponent>(ent);
        _trackedAudio.Add(ent.Owner);

        if (!HasAppliedLocalEffect((ent.Owner, ent.Comp), localEffects))
            localEffects.BaseAuxiliary = ValidateAuxiliary(localEffects.BaseAuxiliary ?? ent.Comp.Auxiliary);
    }

    /// <summary>
    /// Reconciles every currently tracked sound against the latest local echo and muffling state.
    /// </summary>
    public void RefreshAllTracked()
    {
        foreach (var uid in _trackedAudio.ToArray())
        {
            Reconcile(uid);
        }
    }

    /// <summary>
    /// AudioComponent.Global is unreliable for some replicated client sounds.
    /// Fall back to the shared PlayGlobal nullspace spawn pattern.
    /// </summary>
    /// <param name="uid">The audio entity to evaluate.</param>
    /// <param name="audio">Its audio component.</param>
    /// <returns>
    /// <see langword="true"/> when the sound should be treated as global and therefore exempt from SCP local effects.
    /// </returns>
    public bool IsEffectivelyGlobal(EntityUid uid, AudioComponent audio)
    {
        if (audio.Global)
            return true;

        if (!_transformQuery.TryComp(uid, out var xform))
            return false;

        return xform.MapID == MapId.Nullspace;
    }

    /// <summary>
    /// Reconciles a tracked sound by resolving its components internally.
    /// </summary>
    /// <param name="uid">The tracked audio entity to reconcile.</param>
    public void Reconcile(EntityUid uid)
    {
        if (!TryGetTrackedAudio(uid, out var audio, out var localEffects))
            return;

        Reconcile(uid, audio, localEffects);
    }

    /// <summary>
    /// Reconciles the actual auxiliary slot of a tracked sound with the desired SCP local effect state.
    /// </summary>
    /// <param name="uid">The tracked audio entity being updated.</param>
    /// <param name="audio">The audio component that owns the underlying source.</param>
    /// <param name="localEffects">The SCP-local state associated with the source.</param>
    /// <remarks>
    /// The method intentionally performs early exits whenever the desired preset is already active, because it runs on
    /// every tracked sound during client audio updates.
    /// </remarks>
    public void Reconcile(EntityUid uid, AudioComponent audio, AudioLocalEffectsComponent localEffects)
    {
        if (TerminatingOrDeleted(uid))
        {
            Untrack(uid);
            return;
        }

        if (!HasAppliedLocalEffect((uid, audio), localEffects))
        {
            localEffects.BaseAuxiliary = ValidateAuxiliary(audio.Auxiliary);
        }

        if (!audio.Loaded)
            return;

        var ent = (uid, audio);
        var targetPreset = ResolveTargetPreset(uid, audio, localEffects);

        if (targetPreset == null)
        {
            if (localEffects.AppliedLocalPreset == null &&
                audio.Auxiliary == localEffects.BaseAuxiliary)
                return;

            RestoreBaseAuxiliary(ent, localEffects);
            return;
        }

        if (localEffects.AppliedLocalPreset == targetPreset &&
            _effectsManager.HasEffect(ent, targetPreset.Value))
        {
            return;
        }

        if (audio.Auxiliary != null &&
            !_effectsManager.HasEffect(ent, targetPreset.Value))
        {
            _audio.SetAuxiliary(uid, audio, null);
        }

        if (_effectsManager.TryAddEffect(ent, targetPreset.Value))
            localEffects.AppliedLocalPreset = targetPreset;
    }

    /// <summary>
    /// Restores the auxiliary that was present before SCP local effects overrode the source.
    /// </summary>
    /// <param name="ent">The audio entity to restore.</param>
    /// <param name="localEffects">The SCP-local tracking state for the sound.</param>
    private void RestoreBaseAuxiliary(Entity<AudioComponent> ent, AudioLocalEffectsComponent localEffects)
    {
        localEffects.BaseAuxiliary = ValidateAuxiliary(localEffects.BaseAuxiliary);
        _audio.SetAuxiliary(ent.Owner, ent.Comp, localEffects.BaseAuxiliary);
        localEffects.AppliedLocalPreset = null;
    }

    /// <summary>
    /// Checks whether the resolver still owns the auxiliary currently attached to a sound.
    /// </summary>
    /// <param name="ent">The audio entity to inspect.</param>
    /// <param name="localEffects">Its tracked SCP-local state.</param>
    /// <returns>
    /// <see langword="true"/> when the current auxiliary matches the last preset applied by the resolver.
    /// </returns>
    private bool HasAppliedLocalEffect(Entity<AudioComponent> ent, AudioLocalEffectsComponent localEffects)
    {
        return localEffects.AppliedLocalPreset != null &&
               _effectsManager.HasEffect(ent, localEffects.AppliedLocalPreset.Value);
    }

    /// <summary>
    /// Returns the auxiliary only if it still points to a live <see cref="AudioAuxiliaryComponent"/>.
    /// </summary>
    /// <param name="auxiliary">The candidate auxiliary entity.</param>
    /// <returns>The validated auxiliary entity, or <see langword="null"/> when it is no longer usable.</returns>
    private EntityUid? ValidateAuxiliary(EntityUid? auxiliary)
    {
        if (auxiliary == null || !HasComp<AudioAuxiliaryComponent>(auxiliary.Value))
            return null;

        return auxiliary;
    }

    /// <summary>
    /// Registers newly created audio entities for SCP local effects and seeds their initial desired echo state.
    /// </summary>
    /// <param name="ent">The newly added audio component.</param>
    /// <param name="args">The component-add event payload.</param>
    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        EnsureTracked(ent);

        if (!_localEffectsQuery.TryComp(ent.Owner, out var localEffects))
            return;

        localEffects.DesiredEchoPreset = GetConfiguredEchoPreset();
    }

    /// <summary>
    /// Returns the echo preset implied by the current echo-related client cvars.
    /// </summary>
    /// <returns>
    /// The configured echo preset, or <see langword="null"/> when client-side echo is disabled.
    /// </returns>
    private ProtoId<AudioPresetPrototype>? GetConfiguredEchoPreset()
    {
        if (!_isEchoEnabled)
            return null;

        return _strongEchoPresetPreferred
            ? StrongEchoEffectPreset
            : StandardEchoEffectPreset;
    }

    /// <summary>
    /// Resolves which SCP preset, if any, should own the local auxiliary slot for the given sound.
    /// </summary>
    /// <param name="uid">The tracked audio entity.</param>
    /// <param name="audio">Its audio component.</param>
    /// <param name="localEffects">The tracked SCP-local effect state.</param>
    /// <returns>
    /// The preset that should be applied by the resolver, or <see langword="null"/> when no SCP local effect should
    /// own the auxiliary slot.
    /// </returns>
    /// <remarks>
    /// Resolution order is intentionally coarse:
    /// <list type="bullet">
    /// <item><description>Global sounds never receive SCP local effects.</description></item>
    /// <item><description><see cref="AudioOcclusionBand.Silent"/> suppresses SCP local auxiliaries entirely.</description></item>
    /// <item><description><see cref="AudioOcclusionBand.Muffled"/> wins over echo and applies the behind-walls preset.</description></item>
    /// <item><description><see cref="AudioOcclusionBand.Clear"/> allows the currently desired echo preset to pass through.</description></item>
    /// </list>
    /// </remarks>
    private ProtoId<AudioPresetPrototype>? ResolveTargetPreset(
        EntityUid uid,
        AudioComponent audio,
        AudioLocalEffectsComponent localEffects)
    {
        if (IsEffectivelyGlobal(uid, audio))
            return null;

        // Fully silent sources must not keep any SCP local auxiliary attached.
        // Otherwise the direct sound is muted by gain, but the auxiliary effect can still produce an audible tail.
        return localEffects.OcclusionBand switch
        {
            AudioOcclusionBand.Clear => localEffects.DesiredEchoPreset,
            AudioOcclusionBand.Muffled => MufflingEffectPreset,
            AudioOcclusionBand.Silent => null,
            _ => null,
        };
    }

    /// <summary>
    /// Removes an entity from the tracked-audio set.
    /// </summary>
    /// <param name="uid">The audio entity to stop tracking.</param>
    private void Untrack(EntityUid uid)
    {
        _trackedAudio.Remove(uid);
    }
}
