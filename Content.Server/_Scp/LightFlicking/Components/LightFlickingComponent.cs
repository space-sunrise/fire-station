namespace Content.Server._Scp.LightFlicking.Components;

[RegisterComponent]
public sealed partial class LightFlickingComponent : Component
{
    [ViewVariables] public bool Enabled;

    [ViewVariables] public TimeSpan? NextFlickStartChanceTime = null;
    [ViewVariables] public TimeSpan NextFlickTime;

    [ViewVariables] public float DumpedRadius;
    [ViewVariables] public float DumpedEnergy;
}
