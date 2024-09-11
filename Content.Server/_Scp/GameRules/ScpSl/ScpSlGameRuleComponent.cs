using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameRules.ScpSl;

[RegisterComponent]
public sealed partial class ScpSlGameRuleComponent : Component
{
    public int EscapedDClass = 0;
    public int EscapedScientists = 0;


    [DataField]
    public List<EntProtoId> AllowedScps = new List<EntProtoId>();

    [DataField]
    public EntProtoId MogProtoId;

    [DataField]
    public EntProtoId ChaosProtoId;

    [DataField]
    public EntProtoId ScientistProtoId;

    [DataField]
    public EntProtoId SecurityProtoId;

    [DataField]
    public EntProtoId ClassDProtoId;
}
