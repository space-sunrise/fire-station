using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp939Mask;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp939MaskUserComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Mask;
}
