using Content.Client._Scp.Grain;
using Content.Client._Scp.Vignette;
using Content.Shared._Scp.RetroMonitor;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.RetroMonitor;

public sealed class RetroMonitorOverlaySystem : EntitySystem
{
    [Dependency] private readonly GrainOverlaySystem _grain = default!;
    [Dependency] private readonly VignetteOverlaySystem _vignette = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private readonly RetroMonitorOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RetroMonitorViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RetroMonitorViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<RetroMonitorViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);

        _grain.RemoveOverlay();
        _vignette.RemoveOverlay();
    }

    private void OnPlayerDetached(Entity<RetroMonitorViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);

        _grain.AddOverlay();
        _vignette.AddOverlay();
    }
}
