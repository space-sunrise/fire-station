using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Research.Artifact.XAT.TakeDrop;

[RegisterComponent, NetworkedComponent]
public sealed partial class XATTakeDropComponent : Component
{
    [DataField]
    public bool TriggerOnTake;

    [DataField]
    public bool TriggerOnDrop;
}
