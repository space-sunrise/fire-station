using Content.Shared._Scp.RetroMonitor;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.RetroMonitor;

public sealed class RetroMonitorOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly RetroMonitorOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RetroMonitorViewComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RetroMonitorViewComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<RetroMonitorViewComponent> ent, ref PlayerAttachedEvent args)
    {
        if (args.Player != _player.LocalSession)
            return;

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<RetroMonitorViewComponent> ent, ref PlayerDetachedEvent args)
    {
        if (args.Player != _player.LocalSession)
            return;

        _overlayManager.RemoveOverlay(_overlay);
    }
}
