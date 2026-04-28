
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ScpReleaseGas;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpReleaseGasComponent : Component
{
    [DataField]
    public EntProtoId<ActionComponent> ActionProto = "ScpSmokeAction";

    [DataField]
    public Solution SmokeSolution = new("АМН-С227", 40);

    [DataField]
    public float SmokeDuration = 30.0f;

    [DataField]
    public int SmokeSpreadRadius = 10;

    [DataField]
    public EntProtoId SmokeProtoId = "АМН-С227Smoke";

    [ViewVariables]
    public EntityUid? ActionEnt;
}
