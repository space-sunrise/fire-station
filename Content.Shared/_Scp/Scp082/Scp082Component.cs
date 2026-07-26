using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Scp082;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class Scp082Component : Component
{
    /// <summary>
    /// Текущее значение голода SCP-082.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Hunger;

    /// <summary>
    /// Текущее значение злости SCP-082.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Anger;

    /// <summary>
    /// Текущий множитель положительного урона ближнего боя.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float DamageModifier = 1f;

    [DataField]
    public float MaxHunger = 100f;

    [DataField]
    public float AngerPerHunger = 1f;

    [DataField]
    public float MaxAnger = 100f;

    [DataField]
    public float MaxDamageModifier = 1.5f;

    [DataField]
    public TimeSpan HungerUpdateInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public float HungerPerUpdate = 1f;

    /// <summary>
    /// Сколько голода снимает обычная съедобная сущность, например кусок мяса.
    /// </summary>
    [DataField]
    public float MeatHungerRestore = 15f;

    /// <summary>
    /// Сколько голода снимает съеденный труп моба.
    /// </summary>
    [DataField]
    public float CorpseHungerRestore = 100f;

    /// <summary>
    /// При каком уровне злости SCP-082 больше нельзя будет удержать руками
    /// </summary>
    [DataField]
    public float AngerHoldDisable = 60f;

    [DataField]
    public float HighlightHungerThreshold = 60f;

    [DataField]
    public float HumanoidFoodHungerThreshold = 60f;

    [DataField]
    public float HighlightRange = 12f;

    [DataField]
    public float MinimumPopupAnger = 20f;

    [DataField]
    public TimeSpan MinimumPopupInterval = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan MaximumPopupInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public List<LocId> AngerPopupMessages = new()
    {
        "scp082-rage-popup-1",
        "scp082-rage-popup-2",
        "scp082-rage-popup-3"
    };

    [ViewVariables]
    public TimeSpan NextHungerUpdate;

    [ViewVariables]
    public TimeSpan NextAngerPopup;

    [ViewVariables]
    public TimeSpan NextHighlightUpdate;
}
