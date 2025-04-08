﻿using Robust.Client.Graphics;

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

    public bool TryAddOverlay()
    {
        if (OverlayManager.HasOverlay<T>())
            return false;

        AddOverlay();
        return true;
    }

    public void AddOverlay()
    {
        if (!Enabled)
            return;

        OverlayManager.AddOverlay(Overlay);
    }

    public bool TryRemoveOverlay()
    {
        if (!OverlayManager.HasOverlay<T>())
            return false;

        RemoveOverlay();
        return true;
    }

    public void RemoveOverlay()
    {
        OverlayManager.RemoveOverlay(Overlay);
    }

    #endregion
}
