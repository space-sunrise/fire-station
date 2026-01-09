using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp330;

[RegisterComponent]
public sealed partial class Scp330Component : Component
{
    [DataField]
    public EntProtoId CandyPrototype = "Scp330Candy";

    [DataField]
    public int MaxCandies = 10;

    [ViewVariables]
    public int CurrentCandies = 10;

    [DataField(required: true)]
    public DamageSpecifier BaseDamage;

    [DataField]
    public int PunishmentAfter = 2;

    public Dictionary<EntityUid, int> ThiefCounter = new();
}
