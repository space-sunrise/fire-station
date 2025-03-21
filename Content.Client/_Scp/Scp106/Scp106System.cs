using Content.Client.Alerts;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Systems;

namespace Content.Client._Scp.Scp106;

public sealed class Scp106System : SharedScp106System
{
    private const int MaxEssence = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private static void OnUpdateAlert(Entity<Scp106Component> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.Scp106EssenceAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var essence = Math.Clamp(ent.Comp.Essence.Int(), 0, MaxEssence);

        sprite.LayerSetState(Scp106VisualLayers.Digit1, $"{(essence / 100) % 10}");
        sprite.LayerSetState(Scp106VisualLayers.Digit2, $"{(essence / 10) % 10}");
        sprite.LayerSetState(Scp106VisualLayers.Digit3, $"{essence % 10}");
    }
}
