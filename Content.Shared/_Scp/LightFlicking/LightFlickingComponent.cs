using Robust.Shared.GameStates;

namespace Content.Shared._Scp.LightFlicking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LightFlickingComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public bool Enabled;

    [ViewVariables] public TimeSpan? NextFlickStartChanceTime = null;
    [ViewVariables] public TimeSpan NextFlickTime;

    [ViewVariables] public float DumpedRadius;
    [ViewVariables] public float DumpedEnergy;
}
