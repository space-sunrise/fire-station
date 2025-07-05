﻿using Robust.Shared.Configuration;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared._Scp.Shaders;
using Content.Shared._Scp.Shaders.Grain;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common.Grain;

public sealed class GrainOverlaySystem : ComponentOverlaySystem<GrainOverlay, GrainOverlayComponent>
{
    [Dependency] private readonly SharedShaderStrengthSystem _shaderStrength = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new GrainOverlay();

        SubscribeLocalEvent<GrainOverlayComponent, ShaderAdditionalStrengthChanged>(OnAdditionalStrengthChanged);

        _cfg.OnValueChanged(ScpCCVars.GrainToggleOverlay, ToggleGrainOverlay);
        _cfg.OnValueChanged(ScpCCVars.GrainStrength, SetBaseStrength);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.GrainToggleOverlay, ToggleGrainOverlay);
        _cfg.UnsubValueChanged(ScpCCVars.GrainStrength, SetBaseStrength);
    }

    protected override void OnPlayerAttached(Entity<GrainOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        base.OnPlayerAttached(ent, ref args);

        SetBaseStrength(_cfg.GetCVar(ScpCCVars.GrainStrength));
    }

    private void OnAdditionalStrengthChanged(Entity<GrainOverlayComponent> ent, ref ShaderAdditionalStrengthChanged args)
    {
        if (_player.LocalEntity != ent)
            return;

        Overlay.CurrentStrength = ent.Comp.CurrentStrength;
    }

    private void ToggleGrainOverlay(bool option)
    {
        Enabled = option;

        ToggleOverlay();
    }

    private void SetBaseStrength(int value)
    {
        var player = _player.LocalEntity;

        if (!player.HasValue)
            return;

        if (!_shaderStrength.TrySetBaseStrength<GrainOverlayComponent>(player.Value, value, out var component))
            return;

        Overlay.CurrentStrength = component.CurrentStrength;
    }
}
