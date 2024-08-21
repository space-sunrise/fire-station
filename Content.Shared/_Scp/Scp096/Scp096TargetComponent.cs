using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096TargetComponent : Component
{
    //Multiple SCP096 Handler
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> TargetedBy { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public int TimesHitted = 0;

    public float HitTimeAcc = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HitWindow = 4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SleepTime = 30f;

    [DataField]
    public ProtoId<StatusIconPrototype> KillIconPrototype = "Scp096TargetIcon";
}
