using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpPossibilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpPossibilitiesComponent : Component
{
    [DataField]
    public bool CanEjectPilotFromMech = true;

    [DataField]
    public bool OpenContainer = true;

    [DataField]
    public float OpenContainerChance = 1;
}
