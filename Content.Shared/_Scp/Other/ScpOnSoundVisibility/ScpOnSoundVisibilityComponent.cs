using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpOnSoundVisibility;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpOnSoundVisibilityComponent : Component
{
    public const float InitialVisibilityAcc = 0.001f;
    public const float DefaultHideTime = 2.5f;
    public const float DefaultMinValue = 40f;
    public const float DefaultMaxValue = 400f;

    [DataField]
    public float HideTime = DefaultHideTime;

    [DataField]
    public float MinValue = DefaultMinValue;

    [DataField]
    public float MaxValue = DefaultMaxValue;
}
