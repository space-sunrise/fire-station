using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common;

/// <summary>
/// Абстрактная прослойка над <see cref="BaseOverlaySystem"/>, которая добавляет оверлей при заходе игрока в сущность
/// с определенным компонентом <see cref="TC"/>
/// </summary>
/// <typeparam name="T">Оверлей, которым будет оперировать система</typeparam>
/// <typeparam name="TC">Компонент, который будет требоваться, чтобы получить оверлей <see cref="T"/></typeparam>
/// <seealso cref="BaseOverlaySystem"/>
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
