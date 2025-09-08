using Robust.Shared.Configuration;

namespace Content.Shared._Scp.ScpCCVars;

[CVarDefs]
public sealed partial class ScpCCVars
{
    /**
     * Shader
     */

    /// <summary>
    /// Выключен ли шейдер зернистости?
    /// </summary>
    public static readonly CVarDef<bool> GrainToggleOverlay =
        CVarDef.Create("shader.grain_toggle_overlay", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Сила шейдера зернистости
    /// </summary>
    public static readonly CVarDef<int> GrainStrength =
        CVarDef.Create("shader.grain_strength", 140, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Будет ли использовать альтернативный метод просчета сущностей для поля зрения
    /// </summary>
    public static readonly CVarDef<bool> FieldOfViewUseAltMethod =
        CVarDef.Create("shader.field_of_view_use_alt_method", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Размер текстуры размытия у шейдера поля зрения
    /// </summary>
    public static readonly CVarDef<float> FieldOfViewBlurScale =
        CVarDef.Create("shader.field_of_view_blur_scale", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Скорость проверки для изменения видимости спрайтов
    /// </summary>
    public static readonly CVarDef<float> FieldOfViewCheckCooldown =
        CVarDef.Create("shader.field_of_view_check_cooldown", 0.1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Прозрачность наложения поля зрения
    /// </summary>
    public static readonly CVarDef<float> FieldOfViewOpacity =
        CVarDef.Create("shader.field_of_view_opacity", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Режим совместимости
     */

    /// <summary>
    /// Будет ли игроку показываться окно с предупреждением о включенном режиме совместимости?
    /// Настраивается, потому что не у всех игроков может ВООБЩЕ работать игра без режима совместимости.
    /// </summary>
    public static readonly CVarDef<bool> CompatibilityModeShowWarning =
        CVarDef.Create("scp.Compatibility_mode_show_warning", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Будут ли использоваться пораженные шейдеры, если включен режим совместимости?
    /// В случае, если игрок не может выключить режим совместимости, то лучше дать ему выбор. Использовать шейдеры или нет.
    /// </summary>
    public static readonly CVarDef<bool> CompatibilityModeUseShaders =
        CVarDef.Create("scp.Compatibility_mode_use_shaders", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Свечение лампочек
     */

    public static readonly CVarDef<bool> EnableLightsGlowing =
        CVarDef.Create("light.enable_lights_glowing", true, CVar.CLIENTONLY | CVar.ARCHIVE);

}
