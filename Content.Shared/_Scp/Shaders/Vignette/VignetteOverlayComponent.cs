using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Shaders.Vignette;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class VignetteOverlayComponent : Component, IShaderStrength
{
    /// <inheritdoc/>
    [ViewVariables]
    public int BaseStrength { get; set; } = 100;

    /// <inheritdoc/>
    [AutoNetworkedField, ViewVariables]
    public int AdditionalStrength { get; set; }

    /// <inheritdoc/>
    [ViewVariables]
    public int CurrentStrength => BaseStrength + AdditionalStrength;
}
