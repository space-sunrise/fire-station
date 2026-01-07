using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist; // Для EntityWhitelist
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class Scp012Component : Component
{
    [DataField] public float Range = 7.0f;
    [DataField] public float AttractionForce = 1.5f;
    [DataField] public float SuicideTimer = 30.0f;
    [DataField] public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2> { { "Slash", FixedPoint2.New(5) } }
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextDamageTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly TimeSpan DamageCooldown = TimeSpan.FromSeconds(2.0);

    // Новые поля для вайтлиста
    [DataField] public EntityWhitelist? Whitelist;
    [DataField] public EntityWhitelist? Blacklist;
}

[RegisterComponent]
public sealed partial class Scp012VictimComponent : Component
{
    [ViewVariables] public EntityUid Source = EntityUid.Invalid;
    [ViewVariables(VVAccess.ReadWrite)] public float TotalTime = 0f;
    [ViewVariables(VVAccess.ReadWrite)] public float SpeakTimer = 0f;
    [ViewVariables(VVAccess.ReadWrite)] public TimeSpan? NextLosCheckTime;
    [ViewVariables] public bool CachedLos = false;

    [DataField] // Убрали "phrases"
    public List<string> Phrases = new()
    {
        "scp012-phrase-1", "scp012-phrase-2", "scp012-phrase-3",
        "scp012-phrase-4", "scp012-phrase-5", "scp012-phrase-6"
    };
}