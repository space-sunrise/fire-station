using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp330;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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

    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntityUid, int> ThiefCounter = new();

    /// <summary>
    /// Список доступных реагентов для эффектов конфет SCP-330
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> AvailableReagents = new ()
    {
        "Scp330Vomit",
        "Scp330PoisonHeal",
        "Scp330CalmDown",
        "Scp330Nothing",
        "Scp330Paralysis",
    };

    /// <summary>
    /// Словарь, связывающий тип конфеты с назначенным реагентом эффекта
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntProtoId, ProtoId<ReagentPrototype>> CandyEffects = new ();
}
