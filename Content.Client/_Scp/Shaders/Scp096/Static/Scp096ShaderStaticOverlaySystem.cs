using Content.Client._Scp.Shaders.Common;
using Content.Shared._Scp.Scp096.Main.Components;

namespace Content.Client._Scp.Shaders.Scp096.Static;

public sealed class Scp096ShaderStaticOverlaySystem : ComponentOverlaySystem<Scp096ShaderStaticOverlay, Scp096ShaderStaticComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        Overlay = new Scp096ShaderStaticOverlay();
    }
}
