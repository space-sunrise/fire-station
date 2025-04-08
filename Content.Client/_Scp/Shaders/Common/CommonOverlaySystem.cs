using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common;

public abstract class CommonOverlaySystem<T> : BaseOverlaySystem<T> where T : Overlay
{
    public override void Initialize()
    {
        base.Initialize();

        // Мир если бы сендбокса не существовало
        // Overlay = new T();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    protected virtual void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    protected virtual void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }
}
