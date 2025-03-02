using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class PillSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PillComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, PillComponent component, ref AfterAutoHandleStateEvent args)
    {
        // Fire edit start - поддержка нестандартных спрайтов пилюль
        if (!component.UseStandardVisuals)
            return;
        // Fire edit end

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!sprite.TryGetLayer(0, out var layer))
            return;

        layer.SetState($"pill{component.PillType + 1}");
    }
}
