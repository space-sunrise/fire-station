namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class Scp012VictimComponent : Component
{
    [ViewVariables]
    public EntityUid? Source;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeakTimer;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextLosCheckTime;

    [ViewVariables]
    public bool CachedLos = false;

    [DataField]
    public List<string> Phrases = new()
    {
        "scp012-phrase-1",
        "scp012-phrase-2",
        "scp012-phrase-3",
        "scp012-phrase-4",
        "scp012-phrase-5",
        "scp012-phrase-6",
    };
}
