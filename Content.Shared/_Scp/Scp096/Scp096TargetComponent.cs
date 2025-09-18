using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096TargetComponent : Component
{
    // Multiple SCP096 Handler
    [AutoNetworkedField, ViewVariables]
    public HashSet<EntityUid> TargetedBy = [];

    [DataField]
    public float SleepTime = 30f;

    [DataField]
    public ProtoId<FactionIconPrototype> KillIconPrototype = "Scp096TargetIcon";
}
