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

}
