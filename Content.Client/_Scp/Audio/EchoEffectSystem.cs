using System.Linq;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

/// <summary>
/// Maintains the desired client-side echo preset for every tracked SCP audio source.
/// </summary>
/// <remarks>
/// This system never applies auxiliaries directly.
/// It only updates the desired echo preset stored in <see cref="Content.Client._Scp.Audio.Components.AudioLocalEffectsComponent"/>.
/// The final decision about whether echo, muffling, or no local auxiliary should be active is delegated to
/// <see cref="AudioEffectResolverSystem"/>, which merges echo state with occlusion-derived muffling state.
/// </remarks>
public sealed class EchoEffectSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectResolverSystem _resolver = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    /// Default environmental preset requested when SCP echo is enabled.
    /// </summary>
    private static readonly ProtoId<AudioPresetPrototype> StandardEchoEffectPreset = "Bathroom";

    /// <summary>
    /// Stronger environmental preset requested when the user opts into the more aggressive variant.
    /// </summary>
    private static readonly ProtoId<AudioPresetPrototype> StrongEchoEffectPreset = "SewerPipe";

    /// <summary>
    /// Cached state of <see cref="ScpCCVars.EchoEnabled"/> used to avoid repeated cvar lookups while processing sounds.
    /// </summary>
    private bool _isClientSideEnabled;

    /// <summary>
    /// Cached state of <see cref="ScpCCVars.EchoStrongPresetPreferred"/>.
    /// </summary>
    private bool _strongPresetPreferred;

    /// <summary>
    /// Reads the initial echo settings and subscribes to runtime changes.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _isClientSideEnabled = _cfg.GetCVar(ScpCCVars.EchoEnabled);
        _strongPresetPreferred = _cfg.GetCVar(ScpCCVars.EchoStrongPresetPreferred);

        _cfg.OnValueChanged(ScpCCVars.EchoEnabled, OnEnabledToggled);
        _cfg.OnValueChanged(ScpCCVars.EchoStrongPresetPreferred, OnPreferredPresetToggled);
    }

    /// <summary>
    /// Removes the cvar subscriptions established during <see cref="Initialize"/>.
    /// </summary>
    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.EchoEnabled, OnEnabledToggled);
        _cfg.UnsubValueChanged(ScpCCVars.EchoStrongPresetPreferred, OnPreferredPresetToggled);
    }

    /// <summary>
    /// Updates the cached enabled flag and reapplies the desired echo preset to every tracked sound.
    /// </summary>
    /// <param name="enabled">Whether the client-side SCP echo feature is enabled.</param>
    private void OnEnabledToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;
        RefreshAllTracked();
    }

    /// <summary>
    /// Updates the cached preset preference and reapplies the desired echo preset to every tracked sound.
    /// </summary>
    /// <param name="useStrong">
    /// <see langword="true"/> to prefer <see cref="StrongEchoEffectPreset"/>; otherwise
    /// <see cref="StandardEchoEffectPreset"/> is used.
    /// </param>
    private void OnPreferredPresetToggled(bool useStrong)
    {
        _strongPresetPreferred = useStrong;
        RefreshAllTracked();
    }

    /// <summary>
    /// Recomputes desired echo state for all currently tracked audio sources and asks the resolver to reconcile them.
    /// </summary>
    /// <remarks>
    /// This is primarily used after a cvar change so the active auxiliary state converges immediately without waiting
    /// for a new sound to spawn.
    /// </remarks>
    private void RefreshAllTracked()
    {
        foreach (var uid in _resolver.TrackedAudio.ToArray())
        {
            RefreshDesiredEcho(uid);
            _resolver.Reconcile(uid);
        }
    }

    /// <summary>
    /// Writes the currently preferred echo preset, or clears the request entirely when echo is disabled.
    /// </summary>
    /// <param name="uid">The tracked audio entity whose desired echo state should be updated.</param>
    private void RefreshDesiredEcho(EntityUid uid)
    {
        if (_isClientSideEnabled)
        {
            _resolver.SetDesiredEchoPreset(uid, GetPreferredPreset());
            return;
        }

        _resolver.SetDesiredEchoPreset(uid, default);
    }

    /// <summary>
    /// Returns the echo preset that matches the current client preference.
    /// </summary>
    /// <returns>The prototype id of the standard or strong echo preset.</returns>
    private ProtoId<AudioPresetPrototype> GetPreferredPreset()
    {
        return _strongPresetPreferred
            ? StrongEchoEffectPreset
            : StandardEchoEffectPreset;
    }
}
