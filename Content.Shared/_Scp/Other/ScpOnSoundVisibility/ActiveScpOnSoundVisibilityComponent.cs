using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class ActiveScpOnSoundVisibilityComponent : Component
{
    [ViewVariables]
    public float VisibilityAcc = ScpOnSoundVisibilityComponent.InitialVisibilityAcc;

    [AutoNetworkedField]
    public uint VisibilityResetCounter;

    [DataField, AutoNetworkedField]
    public float HideTime = ScpOnSoundVisibilityComponent.DefaultHideTime;

    [DataField, AutoNetworkedField]
    public int MinValue = ScpOnSoundVisibilityComponent.DefaultMinValue;

    [DataField, AutoNetworkedField]
    public int MaxValue = ScpOnSoundVisibilityComponent.DefaultMaxValue;

    [NonSerialized]
    public uint LastHandledVisibilityResetCounter;
}
