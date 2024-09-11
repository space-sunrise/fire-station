using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp939;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp939Component : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution SmokeSolution { get; set; } = new("АМН-С227", 200);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SmokeDuration { get; set; } = 30.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SmokeSpreadRadius { get; set; } = 10;

    [DataField]
    public EntProtoId SmokeProtoId = "АМН-С227Smoke";

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "Scp939Mimic",
        "Scp939Smoke",
        "Scp939Sleep",
    };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HibernationDuration = 60.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HibernationHealingRate = new()
    {
        DamageDict = new ()
        {
            { "Blunt", -9.0f },
            { "Slash", -9.0f},
            { "Piercing", -9.0f },
            { "Heat", -9.0f },
            { "Shock", -9.0f },
        }
    };
}
