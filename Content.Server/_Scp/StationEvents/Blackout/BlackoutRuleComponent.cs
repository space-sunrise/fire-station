namespace Content.Server._Scp.StationEvents.Blackout;

[RegisterComponent]
public sealed partial class BlackoutRuleComponent : Component
{
    public readonly List<EntityUid> Powered = new();

    public readonly float SecondsUntilOff = 30.0f;

    public int NumberPerSecond = 0;
    public float UpdateRate => 1.0f / NumberPerSecond;
    public float FrameTimeAccumulator = 0.0f;
}
