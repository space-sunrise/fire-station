using Robust.Shared.GameStates;

namespace Content.Shared._Scp.LightFlicking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LightFlickingComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public bool Enabled;

    [ViewVariables] public TimeSpan? NextFlickStartChanceTime = null;
    [ViewVariables] public TimeSpan NextFlickTime;

    [AutoNetworkedField, ViewVariables] public float DumpedRadius;
    [AutoNetworkedField, ViewVariables] public float DumpedEnergy;
    [AutoNetworkedField, ViewVariables] public Color DumpedColor;
}
