using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp035;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp035MaskUserComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Mask;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Servants = new();

    [AutoNetworkedField]
    [DataField]
    public EntProtoId ServantsProto = "MobServant035";

    [AutoNetworkedField]
    [DataField]
    public int MaxServants = 3;

    [AutoNetworkedField]
    [DataField]
    public EntProtoId DeadSpawnProto = "Ash";

    [AutoNetworkedField]
    [DataField]
    public float MeleeDamageModificator = 4;

    [AutoNetworkedField]
    [DataField]
    public TimeSpan ActionStunDuration = TimeSpan.FromSeconds(10);

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public MaskOrderType CurrentOrder = MaskOrderType.Follow;

    [AutoNetworkedField]
    public EntityUid ActionRaiseArmy;

    [AutoNetworkedField]
    public EntityUid ActionOrderStayEntity;

    [AutoNetworkedField]
    public EntityUid ActionOrderFollowEntity;

    [AutoNetworkedField]
    public EntityUid ActionOrderKillEmEntity;

    [AutoNetworkedField]
    public EntityUid ActionOrderLooseEntity;

    [AutoNetworkedField]
    public EntityUid ActionStunEntity;
}
