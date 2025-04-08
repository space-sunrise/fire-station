using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common;

public abstract class ComponentOverlaySystem<T, TC> : BaseOverlaySystem<T> where T : Overlay where TC : IComponent
{
    public override void Initialize()
    {
        base.Initialize();

        // Мир если бы сендбокса не существовало
        // Overlay = new T();

        SubscribeLocalEvent<TC, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TC, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    protected virtual void OnPlayerAttached(Entity<TC> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    protected virtual void OnPlayerDetached(Entity<TC> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }
}
