using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp049;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp049Component : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Minions = new();

    [DataField]
    public List<ProtoId<EntityPrototype>> Actions = new()
    {
        "ActionScp049Resurrect",
        "ActionScp049KillResurrected",
        "ActionScp049KillLeavingBeing",
        "ActionScp049SelfHeal",
        "ActionScp049HealMinion",
    };

    [DataField]
    public static ProtoId<FactionIconPrototype> Icon = "Scp049StatusIcon";

    [DataField]
    public TimeSpan ResurrectionTime = TimeSpan.FromSeconds(20);

    [DataField]
    public List<EntProtoId> SurgeryTools = new()
    {
        "Cautery", "Drill", "Scalpel", "Retractor", "Hemostat", "Saw",
    };

    public EntProtoId NextTool = default!;
}
