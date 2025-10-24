using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Blood;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodLineAnimatedComponent : Component
{
    [ViewVariables]
    public static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(5f);
}
