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
            { "Blunt", -20.0f },
            { "Slash", -20.0f },
            { "Piercing", -20.0f },
            { "Heat", -20.0f },
            { "Shock", -20.0f },
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
