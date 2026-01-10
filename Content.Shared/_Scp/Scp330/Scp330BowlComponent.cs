using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp330;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp330BowlComponent : Component
{
    [DataField]
    public string ContainerId = "bowl";

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true)]
    public DamageSpecifier BaseDamage;

    [DataField]
    public int PunishmentAfter = 2;

    [DataField]
    public float BasePunishmentRadius = 1.5f;

    [DataField]
    public Color ExamineMessageColor = Color.Gray;

    [ViewVariables]
    public Dictionary<EntityUid, int> ThiefCounter = new();
}
