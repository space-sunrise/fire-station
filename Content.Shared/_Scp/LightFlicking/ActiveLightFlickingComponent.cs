using Robust.Shared.GameStates;

namespace Content.Shared._Scp.LightFlicking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveLightFlickingComponent : Component
{
    [ViewVariables] public TimeSpan NextFlickTime;

    [AutoNetworkedField, ViewVariables] public float CachedRadius;
    [AutoNetworkedField, ViewVariables] public float CachedEnergy;
    [AutoNetworkedField, ViewVariables] public Color CachedColor;
}
