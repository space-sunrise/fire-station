using Robust.Shared.Configuration;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared._Scp.Shaders.Grain;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.Common.Grain;

public sealed class GrainOverlaySystem : ComponentOverlaySystem<GrainOverlay, GrainOverlayComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new GrainOverlay();

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

        TrySetBaseStrength(player.Value, value);
    }

    /// <summary>
    /// Устанавливает базовую силу шейдера зернистости: <see cref="GrainOverlayComponent.BaseStrength"/>
    /// </summary>
    /// <param name="ent">Сущность, к которой будет применено значение</param>
    /// <param name="value">Значение, которое будет установлено</param>
    /// <returns>Получилось/Не получилось</returns>
    public bool TrySetBaseStrength(Entity<GrainOverlayComponent?> ent, int value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        ent.Comp.BaseStrength = value;
        Overlay.CurrentStrength = ent.Comp.CurrentStrength;

        return true;
    }
}
