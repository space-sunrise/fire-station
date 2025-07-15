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

        SubscribeLocalEvent<HighlightedComponent, HighLightStartEvent>(OnHighlightStarted);
        SubscribeLocalEvent<HighlightedComponent, HighLightEndEvent>(OnHighlightEnded);
    }

    private void OnHighlightStarted(Entity<HighlightedComponent> ent, ref HighLightStartEvent args)
    {
        if (ent.Comp.Recipient.HasValue && _player.LocalEntity != ent.Comp.Recipient)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        sprite.PostShader = _highlightShader;
    }

    private void OnHighlightEnded(Entity<HighlightedComponent> ent, ref HighLightEndEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.PostShader != _highlightShader)
            return;

        sprite.PostShader = null;
    }
}
