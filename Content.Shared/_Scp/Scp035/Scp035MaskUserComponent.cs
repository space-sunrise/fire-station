using Robust.Shared.GameStates;

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
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxServants = 3;

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
