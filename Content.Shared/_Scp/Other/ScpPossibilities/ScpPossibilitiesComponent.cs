using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ScpPossibilities;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpPossibilitiesComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanEjectPilotFromMech = true;
}
