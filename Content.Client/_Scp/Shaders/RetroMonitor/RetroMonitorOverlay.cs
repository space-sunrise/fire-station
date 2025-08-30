using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.RetroMonitor;

public sealed class RetroMonitorOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _retroShader;

    public RetroMonitorOverlay()
    {
        IoCManager.InjectDependencies(this);
        _retroShader = _prototypeManager.Index<ShaderPrototype>("CRT_VHS").InstanceUnique();

        ZIndex = 20;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _retroShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        handle.UseShader(_retroShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
