using Content.Client._Scp.UI;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common;

public sealed class CompabilityModeActiveWarningSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private CompabilityModeActiveWarningWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _window?.Dispose();
        _window = new CompabilityModeActiveWarningWindow();
        _window.OpenCentered();
    }
}
