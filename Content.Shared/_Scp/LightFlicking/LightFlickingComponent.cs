using Robust.Shared.GameStates;

namespace Content.Shared._Scp.LightFlicking;

[RegisterComponent, NetworkedComponent]
public sealed partial class LightFlickingComponent : Component
{
    [ViewVariables] public TimeSpan? NextFlickStartChanceTime = null;
}
