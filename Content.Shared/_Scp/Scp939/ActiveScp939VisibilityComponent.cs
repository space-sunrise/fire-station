using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp939;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class ActiveScp939VisibilityComponent : Component
{
    [ViewVariables]
    public float VisibilityAcc = Scp939VisibilityComponent.InitialVisibilityAcc;

    [AutoNetworkedField]
    public uint VisibilityResetCounter;

    [DataField, AutoNetworkedField]
    public float HideTime = Scp939VisibilityComponent.DefaultHideTime;

    [DataField, AutoNetworkedField]
    public int MinValue = Scp939VisibilityComponent.DefaultMinValue;

    [DataField, AutoNetworkedField]
    public int MaxValue = Scp939VisibilityComponent.DefaultMaxValue;

    [NonSerialized]
    public uint LastHandledVisibilityResetCounter;
}
