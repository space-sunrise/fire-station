
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
    public TimeSpan HibernationDurationOnCriticalState = TimeSpan.FromSeconds(360);

    [DataField]
    public TimeSpan HibernationDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public bool HibernationOnHibernationState;

    [DataField]
    public List<MobState> HibernationStates = new() { MobState.Critical };

    [DataField]
    public bool HibernationHealing;

    [DataField]
    public DamageSpecifier HibernationHealingRate = new()
    {
        DamageDict = new()
        {
            { "Blunt", -20f },
            { "Slash", -20f },
            { "Piercing", -20f },
            { "Heat", -20f },
            { "Shock", -20f },
            { "Bloodloss", -20f},
            { "Genetic", -20f },
            { "Toxin", -20f },
            { "Airloss", -20f },
            { "Asphyxiation", -20f },
            { "Poison", -20f },
            { "Radiation", -20f },
            { "Cellular", -20f}
        }
    };

    [DataField]
    public int BaseBloodRefreshAmount = 1;

    [DataField]
    public int FellAsleepBloodRefreshAmount = 20;

    [ViewVariables]
    public EntityUid? ActionEnt;
}
