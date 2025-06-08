namespace Content.Server._Scp.LightFlicking;

[RegisterComponent]
public sealed partial class LightFlickingComponent : Component
{
    [ViewVariables] public TimeSpan? NextFlickStartChanceTime = null;
}
