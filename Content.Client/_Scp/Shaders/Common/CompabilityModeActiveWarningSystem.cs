using Content.Client._Scp.Shaders.Common.Grain;
using Content.Client._Scp.Shaders.FieldOfView;
using Content.Client._Scp.UI;
using Content.Shared._Scp.ScpCCVars;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client._Scp.Shaders.Common;

public sealed class CompabilityModeActiveWarningSystem : EntitySystem
{
    [Dependency] private readonly GrainOverlaySystem _grain = default!;
    [Dependency] private readonly FieldOfViewOverlaySystem _fov = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private CompabilityModeActiveWarningWindow? _window;
    public bool IsCompabilityModeEnabled;

    public override void Initialize()
    {
        base.Initialize();

        CheckCompabilityMode();
        _configuration.OnValueChanged(ScpCCVars.CompabilityModeUseShaders, ToggleShaders);
    }

    private void CheckCompabilityMode()
    {
        IsCompabilityModeEnabled = _configuration.GetCVar(CVars.DisplayCompat);

        if (!IsCompabilityModeEnabled)
            return;

        var showWarning = _configuration.GetCVar(ScpCCVars.CompabilityModeShowWarning);
        var useShaders = _configuration.GetCVar(ScpCCVars.CompabilityModeUseShaders);

        if (showWarning)
            ShowWindow();

        if (!useShaders)
            ToggleShaders(false);
    }

    private void ShowWindow()
    {
        if (_window != null)
            return;

        _window = new CompabilityModeActiveWarningWindow();
        _window.OpenCentered();
    }

    private void ToggleShaders(bool value)
    {
        if (!IsCompabilityModeEnabled)
            return;

        _grain.Enabled = value;
        _grain.ToggleOverlay(value);

        _fov.Enabled = value;
        _fov.ToggleOverlay(value);
    }
}
