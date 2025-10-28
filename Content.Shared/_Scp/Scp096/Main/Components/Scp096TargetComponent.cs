using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

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

    [DataField]
    public FixedPoint2 TotalDamageToStop = FixedPoint2.New(500);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AlreadyAppliedDamage = FixedPoint2.Zero;
}
