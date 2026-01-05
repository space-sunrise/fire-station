using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class SCP012Component : Component
{
    [DataField("range")]
    public float Range = 7.0f;

    [DataField("attractionForce")]
    public float AttractionForce = 1.5f;

    [DataField("suicideThreshold")]
    public float SuicideThreshold = 30.0f;

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", FixedPoint2.New(2) }
        }
    };

    public float DamageTimer = 0f;
}

[RegisterComponent]
public sealed partial class SCP012VictimComponent : Component
{
    public EntityUid Source;
    public float TotalTime = 0f;
	public float SpeakTimer = 0f; // таймер для пауз между фразами
}