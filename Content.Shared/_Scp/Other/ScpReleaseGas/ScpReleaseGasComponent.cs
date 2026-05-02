
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ScpReleaseGas;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpReleaseGasComponent : Component
{
    [DataField]
    public EntProtoId<ActionComponent> ActionProto = "ScpSmokeAction";

    [ViewVariables]
    public EntityUid? ActionEnt;
}
