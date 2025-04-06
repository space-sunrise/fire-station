﻿using Robust.Shared.Configuration;

namespace Content.Shared._Scp.ScpCCVars;

[CVarDefs]
public sealed class ScpCCVars
{
    /**
     * Shader
     */

    /// <summary>
    /// Grain shader
    /// </summary>
    public static readonly CVarDef<bool> GrainToggleOverlay = CVarDef.Create("shader.grain_toggle_overlay", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}