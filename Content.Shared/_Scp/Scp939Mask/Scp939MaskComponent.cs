using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp939Mask;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp939MaskComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? User;
}
