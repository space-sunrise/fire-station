using Content.Shared._Scp.Other.ScpSleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Other.ScpSleep;

public sealed class ScpSleepVisualizer : VisualizerSystem<ScpSleepComponent>
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, ScpSleepComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        UpdateSprite(uid);
    }

    private void UpdateSprite(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent) ||
            !TryComp<MobStateComponent>(uid, out var mobStateComponent) ||
            !_spriteSystem.LayerMapTryGet((uid, spriteComponent), ScpSleepLayers.Base, out var layerId, false))
            return;

        if (mobStateComponent.CurrentState is MobState.Dead or MobState.Critical)
        {
            _spriteSystem.LayerSetRsiState(uid, layerId, "dead");
            return;
        }

        _spriteSystem.LayerSetRsiState(uid, layerId, "alive");

        if (_appearanceSystem.TryGetData<bool>(uid, ScpSleepVisuals.Sleeping, out var sleeping) && sleeping)
        {
            _spriteSystem.LayerSetRsiState(uid, layerId, "asleep");
        }
    }
}
