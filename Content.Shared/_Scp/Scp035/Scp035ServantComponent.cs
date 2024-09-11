using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp035;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class Scp035ServantComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? User;
}
