using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Research.Artifact.XAT.ModifyContainer;

[RegisterComponent, NetworkedComponent]
public sealed partial class XATModifyContainerComponent : Component
{
    [DataField]
    public string? ContainerId;

    [DataField]
    public bool TriggerOnInsert;

    [DataField]
    public bool TriggerOnRemove;
}
