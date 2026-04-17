using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Настройки DoAfter для операций SCP-933.
/// Все параметры do-after в одном компоненте для удобства.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933DoAfterSettingsComponent : Component
{
    // === PEEL (отрыв от рулона) ===

    /// <summary>
    /// Время отрыва полоски от рулона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PeelDelay = 3.5f;

    /// <summary>
    /// Минимальное время отрыва (защита от эксплойтов).
    /// </summary>
    public float MinimumPeelDelay => 0.1f;

    // === APPLY (наклейка на лицо) ===

    /// <summary>
    /// Время наклейки на себя.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ApplySelfDelay = 2.5f;

    /// <summary>
    /// Время наклейки на другого.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ApplyOtherDelay = 2.5f;

    /// <summary>
    /// Минимальное время наклейки.
    /// </summary>
    public float MinimumApplyDelay => 0.1f;

    // === RIP (срыв с лица) ===

    /// <summary>
    /// Время срыва с чужого лица.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RipDelay = 2.5f;

    /// <summary>
    /// Время срыва со своего лица (если возможно).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RipSelfDelay = 5.0f;

    /// <summary>
    /// Минимальное время срыва.
    /// </summary>
    public float MinimumRipDelay => 0.1f;

    // === BREAK CONDITIONS ===

    /// <summary>
    /// Прерывать при движении пользователя.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnUserMove = true;

    /// <summary>
    /// Прерывать при движении цели.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnTargetMove = true;

    /// <summary>
    /// Прерывать при получении урона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDamage = true;

    /// <summary>
    /// Прерывать при выбрасывании предмета.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDropItem = true;

    /// <summary>
    /// Прерывать при смене руки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// Требуется ли свободная рука.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedHand = true;

    /// <summary>
    /// Проверка дистанции до цели.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckDistance = true;

    /// <summary>
    /// Максимальная дистанция для операций.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDistance = 1.5f;
}
