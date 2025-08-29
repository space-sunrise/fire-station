using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp049;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp049MinionComponent : Component
{
    [ViewVariables]
    public static readonly ProtoId<FactionIconPrototype> Icon = "Scp049MinionStatusIcon";

    [AutoNetworkedField]
    public EntityUid Scp049Owner;
}
