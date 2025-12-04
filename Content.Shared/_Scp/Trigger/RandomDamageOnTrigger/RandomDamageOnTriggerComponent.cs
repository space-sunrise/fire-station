using Content.Shared.Damage.Prototypes;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Trigger.RandomDamageOnTrigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomDamageOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true)]
    public List<ProtoId<DamageTypePrototype>> DamageTypes;

    [DataField]
    public bool IgnoreResistancesForDamage;

    [DataField]
    public double MaxDamagePercent = 0.95d;

    [DataField]
    public double MinDamagePercent = 0.15d;

    [DataField]
    public float Probability = 0.25f;
}
