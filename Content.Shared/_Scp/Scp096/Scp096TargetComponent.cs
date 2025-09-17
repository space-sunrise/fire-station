using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096TargetComponent : Component
{
    // Multiple SCP096 Handler
    [AutoNetworkedField, ViewVariables]
    public readonly HashSet<EntityUid> TargetedBy = [];

    [ViewVariables]
    public int TimesHitted = 0;

    public float HitTimeAcc = 0f;

    [DataField]
    public float HitWindow = 4f;

    [DataField]
    public float SleepTime = 30f;

    [DataField]
    public ProtoId<FactionIconPrototype> KillIconPrototype = "Scp096TargetIcon";
}
