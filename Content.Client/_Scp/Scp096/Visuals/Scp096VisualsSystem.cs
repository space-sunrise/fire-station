using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.Scp096.Main.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Scp096.Visuals;

public sealed class Scp096VisualsSystem : VisualizerSystem<Scp096Component>
{
    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Info;
    }

    protected override void OnAppearanceChange(EntityUid uid, Scp096Component component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        foreach (var key in args.AppearanceData.Keys)
        {
            if (!AppearanceSystem.TryGetData<bool>(uid, key, out var boolValue, args.Component))
                continue;

            if (key is not Scp096VisualsState)
                continue;

            Log.Debug($"{key.ToString()}: {boolValue}");
            SpriteSystem.LayerSetVisible(uid, key, boolValue);
        }
    }
}
