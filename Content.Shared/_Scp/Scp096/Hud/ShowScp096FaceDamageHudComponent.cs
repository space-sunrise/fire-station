using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Hud;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShowScp096FaceDamageHudComponent : Component
{
    [ViewVariables]
    public static readonly IReadOnlyList<ProtoId<FactionIconPrototype>> Icons = new List<ProtoId<FactionIconPrototype>>
    {
        "Scp096FaceDamage0",
        "Scp096FaceDamage1",
        "Scp096FaceDamage2",
        "Scp096FaceDamage3",
        "Scp096FaceDamage4",
        "Scp096FaceDamage5",
    };
}
