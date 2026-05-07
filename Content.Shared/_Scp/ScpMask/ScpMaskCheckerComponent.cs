using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ScpMask;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpMaskCheckerComponent : Component
{
    [DataField]
    public bool BlockTriggers = true;
}
