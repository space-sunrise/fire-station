using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp012;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class Scp012VictimComponent : Component
{
    [ViewVariables]
    public EntityUid? Source;

    [DataField]
    public float PickupDistance = 0.6f;

    [DataField]
    public TimeSpan SuicideCooldown = TimeSpan.FromSeconds(30f);

    [ViewVariables, AutoPausedField]
    public TimeSpan? NextSuicideTime;

    [DataField]
    public TimeSpan SpeakCooldown = TimeSpan.FromSeconds(4f);

    [ViewVariables, AutoPausedField]
    public TimeSpan? NextSpeakTime;

    [DataField]
    public TimeSpan PassiveDamageCooldown = TimeSpan.FromSeconds(1f);

    [ViewVariables, AutoPausedField]
    public TimeSpan? NextPassiveDamageTime;

    [DataField]
    public float SpeakChance = 0.7f;

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

    [ViewVariables]
    public bool CachedLos;

    [ViewVariables]
    public TimeSpan? NextLosCheckTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LosCooldown = TimeSpan.FromSeconds(0.5f);

    [ViewVariables]
    public EntityUid? AudioStream;
}
