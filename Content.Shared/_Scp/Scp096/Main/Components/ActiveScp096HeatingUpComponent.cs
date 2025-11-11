using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ActiveScp096HeatingUpComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? RageHeatUpEnd;
}
