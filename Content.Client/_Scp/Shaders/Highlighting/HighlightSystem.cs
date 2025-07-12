using Content.Shared._Scp.Shaders.Highlighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.Highlighting;

/// <summary>
/// Система, регулирующая добавление шейдера подсвечивания.
/// </summary>
public sealed class HighlightSystem : SharedHighlightSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    /// <summary>
    /// Шейдер подсвечивания.
    /// Будет накладываться на текстуры используя внутренние методы спрайта.
    /// </summary>
    private ShaderInstance _highlightShader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _highlightShader = _prototype.Index<ShaderPrototype>("HighlightWave").Instance();

        SubscribeLocalEvent<HighlightedComponent, ComponentInit>(OnHighlightAdded);
        SubscribeLocalEvent<HighlightedComponent, ComponentRemove>(OnHighlightRemoved);
    }

    private void OnHighlightAdded(Entity<HighlightedComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Recipient.HasValue && _player.LocalEntity != ent.Comp.Recipient)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        sprite.PostShader = _highlightShader;
    }

    private void OnHighlightRemoved(Entity<HighlightedComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.PostShader != _highlightShader)
            return;

        sprite.PostShader = null;
    }
}
