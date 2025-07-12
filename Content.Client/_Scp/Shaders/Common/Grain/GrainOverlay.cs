using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.Common.Grain;

/// <summary>
/// Шейдер зернистости с заданной силой <see cref="CurrentStrength"/>
/// </summary>
public sealed class GrainOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ShaderInstance _shader;

    /// <summary>
    /// Текущая сила шейдера
    /// </summary>
    public float CurrentStrength;

    public GrainOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index<ShaderPrototype>("Grain").InstanceUnique();
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (CurrentStrength == 0f)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("strength", CurrentStrength);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}
