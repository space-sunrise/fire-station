using Content.Shared._Scp.Other.ScpOnSoundVisibility;

namespace Content.Client._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent]
public sealed partial class ActiveScpOnSoundVisibilityComponent : Component
{
    [ViewVariables]
    public float VisibilityAcc = ScpOnSoundVisibilityComponent.InitialVisibilityAcc;

    public float HideTime = ScpOnSoundVisibilityComponent.DefaultHideTime;

    public float MinValue = ScpOnSoundVisibilityComponent.DefaultMinValue;

    public float MaxValue = ScpOnSoundVisibilityComponent.DefaultMaxValue;

    [ViewVariables]
    public bool OnCollide;
}
