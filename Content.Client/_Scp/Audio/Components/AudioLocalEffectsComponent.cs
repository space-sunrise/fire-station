using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio.Components;

/// <summary>
/// Stores SCP-specific client-side audio effect state for a single tracked <see cref="AudioComponent"/>.
/// </summary>
/// <remarks>
/// This component does not describe the base audio authored by gameplay code.
/// Instead it records transient client-side decisions made by the SCP echo and muffling pipeline, such as the
/// currently requested echo preset, the resolved occlusion band, and the original auxiliary that must be restored
/// when local overrides are no longer needed.
/// </remarks>
[RegisterComponent]
public sealed partial class AudioLocalEffectsComponent : Component
{
    /// <summary>
    /// The current occlusion severity bucket computed by <see cref="Content.Client._Scp.Audio.AudioMuffleSystem"/>.
    /// </summary>
    [ViewVariables]
    public AudioOcclusionBand OcclusionBand;

    /// <summary>
    /// The echo preset currently requested by <see cref="Content.Client._Scp.Audio.EchoEffectSystem"/>.
    /// </summary>
    /// <remarks>
    /// This value represents intent only. The resolver may ignore it when the source is global, muffled, or silent.
    /// </remarks>
    [ViewVariables]
    public ProtoId<AudioPresetPrototype>? DesiredEchoPreset;

    /// <summary>
    /// The SCP preset currently applied by <see cref="Content.Client._Scp.Audio.AudioEffectResolverSystem"/>, if any.
    /// </summary>
    [ViewVariables]
    public ProtoId<AudioPresetPrototype>? AppliedLocalPreset;

    /// <summary>
    /// The auxiliary that was present before SCP-specific client logic overrode the slot.
    /// </summary>
    /// <remarks>
    /// This allows the resolver to restore pre-existing audio routing once local echo or muffling is no longer active.
    /// </remarks>
    [ViewVariables]
    public EntityUid? BaseAuxiliary;
}

/// <summary>
/// High-level audibility buckets derived from raw occlusion.
/// </summary>
/// <remarks>
/// The band is used by the resolver to decide whether the source should receive a muffling preset, an echo preset,
/// or no SCP auxiliary at all.
/// </remarks>
public enum AudioOcclusionBand : byte
{
    /// <summary>
    /// The source is effectively unobstructed and may receive echo if echo is enabled.
    /// </summary>
    Clear,

    /// <summary>
    /// The source is obstructed enough to receive the dedicated behind-walls muffling preset, but it remains audible.
    /// </summary>
    Muffled,

    /// <summary>
    /// The source is treated as fully silent for SCP local effects and should not keep an SCP auxiliary attached.
    /// </summary>
    Silent,
}
