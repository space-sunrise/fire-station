using Robust.Shared.Configuration;
using Content.Shared._Scp.ScpCCVars;

namespace Content.Client._Scp.Shaders.Common.Grain;

public sealed class GrainOverlaySystem : CommonOverlaySystem<GrainOverlay>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new GrainOverlay();

        _cfg.OnValueChanged(ScpCCVars.GrainToggleOverlay, OnGrainToggleOverlayOptionChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.GrainToggleOverlay, OnGrainToggleOverlayOptionChanged);
    }

    private void OnGrainToggleOverlayOptionChanged(bool option)
    {
        Enabled = option;

        ToggleOverlay();
    }
}
