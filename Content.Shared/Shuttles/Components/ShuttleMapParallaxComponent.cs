using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Shows a parallax background on the shuttle map console.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShuttleMapParallaxComponent : Component
{
    public static readonly ResPath FallbackTexture = new ResPath("/Textures/Parallaxes/layer1.png"); // Fire edit - прошлую я удалил

    // TODO: This should ideally be shared with parallax stuff to avoid duplication, for now it's just a texture
    [DataField, AutoNetworkedField]
    public ResPath TexturePath;
}
