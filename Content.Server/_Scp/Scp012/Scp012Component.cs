using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class SCP012Component : Component
{
    [DataField]
    public float Range = 7.0f;

    [DataField]
    public float AttractionForce = 1.5f;

    [DataField]
    public float SuicideThreshold = 30.0f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", FixedPoint2.New(5) } 
        }
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextDamageTime;

    // Интервал между тиками урона
    public readonly TimeSpan DamageCooldown = TimeSpan.FromSeconds(2.0);
}

[RegisterComponent]
public sealed partial class SCP012VictimComponent : Component
{
    public EntityUid Source = EntityUid.Invalid;
    public float TotalTime = 0f;
    public float SpeakTimer = 0f; 
}