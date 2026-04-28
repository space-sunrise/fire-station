
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
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
    public EntProtoId StatusEffect = "StatusEffectForcedSleeping";

    [DataField]
    public SoundSpecifier? CritSound;

    [DataField]
    public float HibernationDurationOnCriticalState = 360f;

    [DataField]
    public float HibernationDuration = 60f;

    [DataField]
    public bool HibernationOnCriticalState = false;


    [DataField]
    public bool HibernationHealing = false;

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
}
