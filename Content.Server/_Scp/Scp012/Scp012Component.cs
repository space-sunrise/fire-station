using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class Scp012Component : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Range = 7.0f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float AttractionForce = 1.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SuicideThreshold = 30.0f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", FixedPoint2.New(5) } 
        }
    };

    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextDamageTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly TimeSpan DamageCooldown = TimeSpan.FromSeconds(2.0);
}

[RegisterComponent]
public sealed partial class Scp012VictimComponent : Component
{
    [ViewVariables]
    public EntityUid Source = EntityUid.Invalid;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeakTimer = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextLosCheckTime;
}