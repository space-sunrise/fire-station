using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp035;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp035MaskComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    public TimeSpan NextMessaging = TimeSpan.Zero;
    public TimeSpan NextLiquidSpawning = TimeSpan.Zero;
}
