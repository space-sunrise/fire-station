using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class ActiveScpOnSoundVisibilityComponent : Component
{
    [ViewVariables]
    public float VisibilityAcc = ScpOnSoundVisibilityComponent.InitialVisibilityAcc;

    [DataField, AutoNetworkedField]
    public float HideTime = ScpOnSoundVisibilityComponent.DefaultHideTime;

    [DataField, AutoNetworkedField]
    public float MinValue = ScpOnSoundVisibilityComponent.DefaultMinValue;

    [DataField, AutoNetworkedField]
    public float MaxValue = ScpOnSoundVisibilityComponent.DefaultMaxValue;

    [ViewVariables]
    public bool OnCollide;
}
