using Content.Client._Scp.Shaders.Common.Grain;
using Content.Client._Scp.Shaders.Common.Vignette;
using Content.Shared._Scp.Scp106;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Shaders.Warping;

public sealed class WarpingOverlaySystem : EntitySystem
{
    [Dependency] private readonly VignetteOverlaySystem _vignette = default!;
    [Dependency] private readonly GrainOverlaySystem _grain = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private WarpOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<WarpingOverlayToggle>(OnToggle);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => Toggle(false));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        Toggle(false);
    }

    private void OnToggle(WarpingOverlayToggle args)
    {
        Toggle(args.Enable);

        // Переключаем лишние шейдеры, их все равно почти не будет видно
        _grain.ToggleOverlay();
        _vignette.ToggleOverlay();
    }

    public void Toggle(bool enable)
    {
        if (enable)
        {
            _overlay ??= new WarpOverlay(_timing.CurTime);
            _overlayManager.AddOverlay(_overlay);
        }
        else if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay.Dispose();
            _overlay = null;
        }
    }
}
