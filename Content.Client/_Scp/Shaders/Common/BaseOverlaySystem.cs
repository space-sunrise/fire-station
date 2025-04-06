using Robust.Client.Graphics;

namespace Content.Client._Scp.Shaders.Common;

public abstract class BaseOverlaySystem<T> : EntitySystem where T : Overlay
{
    [Dependency] protected readonly IOverlayManager OverlayManager = default!;

    protected T Overlay = default!;
    protected bool Enabled = true;

    #region Public API

    public void ToggleOverlay()
    {
        if (OverlayManager.HasOverlay<T>())
            RemoveOverlay();
        else
            AddOverlay();
    }

    public void AddOverlay()
    {
        if (!Enabled)
            return;

        if (OverlayManager.HasOverlay<T>())
            return;

        OverlayManager.AddOverlay(Overlay);
    }

    public void RemoveOverlay()
    {
        if (OverlayManager.HasOverlay<T>())
            OverlayManager.RemoveOverlay(Overlay);
    }

    #endregion
}
