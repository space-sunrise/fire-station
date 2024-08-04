using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Scp.Abilities;

[RegisterComponent]
public sealed partial class BorgResistComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("resistActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ResistActionId = "BorgResist";

    [DataField]
    public EntityUid? ResistActionUid;

    [DataField]
    public float MultReflectionChance = 0.5f;

    [DataField]
    public float SpeedModifier = 0.7f;

    [DataField]
    public float DrainCharge = 10f;

    [DataField]
    public bool Enabled;
}

public sealed partial class BorgResistanceActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum BorgResistVisuals : byte
{
    Shielding
}

[Serializable, NetSerializable]
public sealed class BorgShieldEnabledEvent(NetEntity borg) : EntityEventArgs
{
    public readonly NetEntity Borg = borg;
}

[Serializable, NetSerializable]
public sealed class BorgShieldDisabledEvent(NetEntity borg) : EntityEventArgs
{
    public readonly NetEntity Borg = borg;
}
