using Content.Shared._Scp.Other.Shaders;

namespace Content.Client._Scp.Shaders.Common.Vignette;

public sealed class VignetteOverlaySystem : ComponentOverlaySystem<VignetteOverlay, VignetteOverlayComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        Overlay = new VignetteOverlay();
    }
}
