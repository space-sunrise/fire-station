using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.ScpSleep;

[RegisterComponent]
public sealed partial class ScpHealOnSleepComponent : Component
{
    [DataField]
    public EntProtoId<ActionComponent> ActionProto = "ScpSleepAction";

    [DataField]
    public bool AddAction = true;

    [DataField]
    public EntProtoId StatusEffect = "StatusEffectForcedSleeping";

    [DataField]
    public SoundSpecifier? CritSound;

    [DataField]
    public TimeSpan HibernationDurationOnHibernationState = TimeSpan.FromSeconds(360);

    [DataField]
    public TimeSpan HibernationDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public List<MobState>? HibernationStates;

    [DataField]
    public DamageSpecifier? HibernationHealingRate;

    [DataField]
    public int BaseBloodRefreshAmount = 1;

    [DataField]
    public int FellAsleepBloodRefreshAmount = 20;

    [ViewVariables]
    public EntityUid? ActionEnt;
}
