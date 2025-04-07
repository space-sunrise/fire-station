using Content.Shared._Scp.ScpCCVars;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.Common.Grain;

public sealed class GrainOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly ShaderInstance _shader;

    // Максимальные и минимальные значения силы
    // Эти пороги используются для настроек клиента и позволяет выбрать доступный диапазон
    public const int StrengthMin = 70;
    public const int StrengthMax = 140;

    private float _currentStrength;

    public GrainOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index<ShaderPrototype>("Grain").InstanceUnique();

        _cfg.OnValueChanged(ScpCCVars.GrainStrength, value => _currentStrength = value, true);
    }

    ~GrainOverlay()
    {
        _cfg.UnsubValueChanged(ScpCCVars.GrainStrength, value => _currentStrength = value);
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("strength", _currentStrength);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}
