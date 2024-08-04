using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Scp.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BorgDashComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("dashActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DashActionId = "BorgDash";

    [DataField]
    public EntityUid? DashActionUid;

    [ViewVariables(VVAccess.ReadWrite), DataField("dashSpeed")]
    public float DashSpeed = 7f;

    [ViewVariables(VVAccess.ReadWrite), DataField("maxDash")]
    public float MaxDash = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("doAfterLength")]
    public TimeSpan DoAfterLength = TimeSpan.FromSeconds(1);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 30 },
        },
    };

    [DataField]
    public DamageSpecifier ZombieDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Heat", 50 },
        },
    };

    [DataField]
    public float DashChargeDrop = 300f;

    [DataField]
    public bool IsDashing;

    [DataField]
    public SoundSpecifier DashSound;

    [DataField]
    public SoundSpecifier ThrowHitSound;
}

public sealed partial class BorgDashActionEvent : WorldTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class BorgDashDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public Vector2 TargetVector;
}

[Serializable, NetSerializable]
public enum BorgDashVisuals : byte
{
    NotDashing,
    Dashing
}
