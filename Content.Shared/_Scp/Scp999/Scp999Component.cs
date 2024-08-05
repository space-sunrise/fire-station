using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp999;

[RegisterComponent]
public sealed partial class Scp999Component : Component
{
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId WallAction;

    [DataField("wallActionEntity"), ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? WallActionEntity;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId RestAction;

    [DataField("restActionEntity"), ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? RestActionEntity;

    [DataField]
    public Scp999States CurrentState = Scp999States.Default;

    [DataField("states")]
    public Dictionary<Scp999States, string> States = new();
}
