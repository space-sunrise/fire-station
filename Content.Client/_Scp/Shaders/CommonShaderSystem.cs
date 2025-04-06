using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders;

public abstract class CommonShaderSystem<T> : EntitySystem where T : Overlay
{
    [Dependency] protected readonly IOverlayManager OverlayManager = default!;

    protected T Overlay = default!;
    protected bool Enabled = true;

    public override void Initialize()
    {
        base.Initialize();

        // Мир если бы сендбокса не существовало
        // Overlay = new T();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

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
