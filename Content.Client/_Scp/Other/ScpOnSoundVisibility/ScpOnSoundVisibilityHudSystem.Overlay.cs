using Content.Shared._Scp.Other.ScpOnSoundVisibility;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Other.ScpOnSoundVisibility;

public sealed partial class ScpOnSoundVisibilityHudSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private void InitializeOverlay()
    {
        SubscribeLocalEvent<ScpOnSoundVisibilityViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ScpOnSoundVisibilityViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<ScpOnSoundVisibilityViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<ScpOnSoundVisibilityViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void AddOverlays()
    {
        if (_overlaysPresented)
            return;

        _overlayManager.AddOverlay(_setAlphaOverlay);
        _overlayManager.AddOverlay(_resetAlphaOverlay);

        _overlaysPresented = true;
    }

    private void RemoveOverlays()
    {
        if (!_overlaysPresented)
            return;

        _overlayManager.RemoveOverlay(_setAlphaOverlay);
        _overlayManager.RemoveOverlay(_resetAlphaOverlay);

        CachedBaseAlphas.Clear();
        _overlaysPresented = false;
    }
}
