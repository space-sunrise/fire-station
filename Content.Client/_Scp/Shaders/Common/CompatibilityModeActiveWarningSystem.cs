using Content.Client._Scp.Shaders.Common.Grain;
using Content.Client._Scp.Shaders.FieldOfView;
using Content.Client._Scp.UI;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client._Scp.Shaders.Common;

public sealed class CompatibilityModeActiveWarningSystem : EntitySystem
{
    [Dependency] private readonly GrainOverlaySystem _grain = default!;
    [Dependency] private readonly FieldOfViewOverlaySystem _fov = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private CompatibilityModeActiveWarningWindow? _window;

    public bool IsCompatibilityModeEnabled { get; private set; }
    public bool CompabilityUseShaders { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        CheckCompatibilityMode();
        _configuration.OnValueChanged(ScpCCVars.CompatibilityModeUseShaders, ToggleShaders);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _configuration.UnsubValueChanged(ScpCCVars.CompatibilityModeUseShaders, ToggleShaders);
    }

    private void CheckCompatibilityMode()
    {
        IsCompatibilityModeEnabled = _configuration.GetCVar(CVars.DisplayCompat);

        if (!IsCompatibilityModeEnabled)
            return;

        var showWarning = _configuration.GetCVar(ScpCCVars.CompatibilityModeShowWarning);
        var useShaders = _configuration.GetCVar(ScpCCVars.CompatibilityModeUseShaders);

        if (showWarning)
            ShowWindow();

        if (!useShaders)
            ToggleShaders(false);
    }

    private void ShowWindow()
    {
        if (_window != null)
            return;

        _window = new CompatibilityModeActiveWarningWindow();
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
    }

    private void ToggleShaders(bool value)
    {
        CompabilityUseShaders = value;

        if (!IsCompatibilityModeEnabled)
            return;

        _grain.Enabled = value;
        _grain.ToggleOverlay(value);

        _fov.Enabled = value;
        _fov.ToggleOverlay(value);
    }
}
