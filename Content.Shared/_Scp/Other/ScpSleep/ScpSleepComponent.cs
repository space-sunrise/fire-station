
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ScpSleep;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpSleepComponent : Component
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
