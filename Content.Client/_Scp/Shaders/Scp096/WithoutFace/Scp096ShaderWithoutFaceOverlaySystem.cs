using Content.Client._Scp.Shaders.Common;
using Content.Shared._Scp.Scp096.Main.Components;

namespace Content.Client._Scp.Shaders.Scp096.WithoutFace;

public sealed class Scp096ShaderWithoutFaceOverlaySystem : ComponentOverlaySystem<Scp096ShaderWithoutFaceOverlay, Scp096ShaderWithoutFaceComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        Overlay = new Scp096ShaderWithoutFaceOverlay();
    }
}
