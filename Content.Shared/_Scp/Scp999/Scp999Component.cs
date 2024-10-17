using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp999;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp999Component : Component
{
    [DataField, AutoNetworkedField]
    public Scp999States CurrentState = Scp999States.Default;

    [DataField]
    public Dictionary<Scp999States, string> States = new();
}
