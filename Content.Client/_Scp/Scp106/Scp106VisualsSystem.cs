using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Scp106;

public sealed class Scp106VisualsSystem : VisualizerSystem<Scp106VisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, Scp106VisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, Scp106Visuals.Visuals, out Scp106VisualsState visualState, args.Component))
            visualState = Scp106VisualsState.Default;

        var sprite = (uid, args.Sprite);

        switch (visualState)
        {
            case Scp106VisualsState.Entering:
                SetAnimatedState(sprite, component, component.EnteringState);
                break;

            case Scp106VisualsState.Exiting:
                SetAnimatedState(sprite, component, component.ExitingState);
                break;

            default:
                SetStaticState(sprite, component, component.DefaultState);
                break;
        }
    }

    private void SetAnimatedState(Entity<SpriteComponent> sprite, Scp106VisualsComponent component, string state)
    {
        SpriteSystem.LayerSetRsiState(sprite.AsNullable(), component.BaseLayer, state);
        SpriteSystem.LayerSetAutoAnimated(sprite.AsNullable(), component.BaseLayer, true);
    }

    private void SetStaticState(Entity<SpriteComponent> sprite, Scp106VisualsComponent component, string state)
    {
        SpriteSystem.LayerSetRsiState(sprite.AsNullable(), component.BaseLayer, state);
        SpriteSystem.LayerSetAutoAnimated(sprite.AsNullable(), component.BaseLayer, false);
    }
}
