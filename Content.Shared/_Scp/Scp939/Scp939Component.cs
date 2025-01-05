using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp939;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp939Component : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution SmokeSolution = new("АМН-С227", 200);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SmokeDuration = 30.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SmokeSpreadRadius = 10;

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

    #region Vision

    [DataField, AutoNetworkedField]
    public bool PoorEyesight;

    [DataField, AutoNetworkedField]
    public float PoorEyesightTime = 10f; // Секунды

    [AutoNetworkedField]
    public TimeSpan? PoorEyesightTimeStart; // Когда начали плохо видеть

    #endregion

    [DataField]
    public int MaxRememberedMessages = 20;

    /// <summary>
    /// Запомненые объектом слова. Ключ - сказанная фраза, значение - пара, в которой ключ имя сказавшего и значение прототип его ттса
    /// </summary>
    [ViewVariables]
    public Dictionary<string, KeyValuePair<string, string?>> RememberedMessages = new();

}
