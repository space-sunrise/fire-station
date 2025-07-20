﻿using Robust.Shared.Configuration;

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
    /// Прозрачность наложения поля зрения
    /// </summary>
    public static readonly CVarDef<float> FieldOfViewOpacity =
        CVarDef.Create("shader.field_of_view_opacity", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
