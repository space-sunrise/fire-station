using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveScp096HeatingUpComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? RageHeatUpEnd;
}
