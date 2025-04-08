namespace Content.Client._Scp.Shaders.Common.Vignette;

public sealed class VignetteOverlaySystem : CommonOverlaySystem<VignetteOverlay>
{
    public override void Initialize()
    {
        base.Initialize();

        Overlay = new VignetteOverlay();
    }
}
