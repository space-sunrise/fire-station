using Robust.Shared.Configuration;

namespace Content.Shared._Scp.ScpCCVars;

public sealed partial class ScpCCVars
{
    /*
     * Echo
     */

    /// <summary>
    /// Enables the client-side echo effect.
    /// </summary>
    public static readonly CVarDef<bool> EchoEnabled =
        CVarDef.Create("scp.echo_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Prefers the stronger echo preset when the echo effect is enabled.
    /// </summary>
    public static readonly CVarDef<bool> EchoStrongPresetPreferred =
        CVarDef.Create("scp.echo_strong_preset_preferred", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Audio muffling
     */

    /// <summary>
    /// Enables client-side audio muffling based on occlusion.
    /// </summary>
    public static readonly CVarDef<bool> AudioMufflingEnabled =
        CVarDef.Create("scp.audio_muffling_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Controls how aggressively occlusion attenuates gain through the exponential falloff curve.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingOcclusionGainFalloff =
        CVarDef.Create("scp.audio_muffling_occlusion_gain_falloff", 0.25f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Occlusion value at or above which the sound is treated as fully silent.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingSilentOcclusionThreshold =
        CVarDef.Create("scp.audio_muffling_silent_occlusion_threshold", 25f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Minimum gain multiplier before the sound is clamped to silence.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingMinAudibleGainFactor =
        CVarDef.Create("scp.audio_muffling_min_audible_gain_factor", 0.001f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Occlusion threshold for applying the muffling effect preset.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingEffectApplyOcclusionThreshold =
        CVarDef.Create("scp.audio_muffling_effect_apply_occlusion_threshold", 1.25f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Occlusion threshold for removing the muffling effect preset.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingEffectClearOcclusionThreshold =
        CVarDef.Create("scp.audio_muffling_effect_clear_occlusion_threshold", 0.9f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Base occlusion contribution for solid blockers.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingSolidBaseOcclusion =
        CVarDef.Create("scp.audio_muffling_solid_base_occlusion", 6f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Additional occlusion contribution per meter of penetration through solid blockers.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingSolidOcclusionPerMeter =
        CVarDef.Create("scp.audio_muffling_solid_occlusion_per_meter", 7f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Base occlusion contribution for transparent blockers.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingTransparentBaseOcclusion =
        CVarDef.Create("scp.audio_muffling_transparent_base_occlusion", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Additional occlusion contribution per meter of penetration through transparent blockers.
    /// </summary>
    public static readonly CVarDef<float> AudioMufflingTransparentOcclusionPerMeter =
        CVarDef.Create("scp.audio_muffling_transparent_occlusion_per_meter", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
