using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp049;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp049Component : Component
{
    [AutoNetworkedField, ViewVariables]
    public HashSet<EntityUid> Minions = [];

    [DataField]
    public HashSet<EntProtoId> Actions =
    [
        "ActionScp049Resurrect",
        "ActionScp049KillResurrected",
        "ActionScp049KillLeavingBeing",
        "ActionScp049SelfHeal",
        "ActionScp049HealMinion",
    ];

    [DataField]
    public TimeSpan ResurrectionTime = TimeSpan.FromSeconds(20);

    [DataField]
    public HashSet<EntProtoId> SurgeryTools =
    [
        "Cautery", "Drill", "Scalpel", "Retractor", "Hemostat", "Saw",
    ];

    [ViewVariables]
    public EntProtoId NextTool = default!;

    public static readonly ProtoId<FactionIconPrototype> Icon = "Scp049StatusIcon";
}
