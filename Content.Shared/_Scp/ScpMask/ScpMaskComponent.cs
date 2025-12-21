using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ScpMask;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ScpMaskComponent : Component, IClothingSlots
{
    /// <summary>
    /// Вайтлист целей, на которых можно надеть маску
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist TargetWhitelist = default!;

    /// <summary>
    /// Слот, в котором должна находиться маска
    /// </summary>
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.MASK;

    /// <summary>
    /// Время, которое объект будет разрывать маску
    /// </summary>
    [DataField]
    public TimeSpan TearTime = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// Шанс того, что маска слетит при получении урона
    /// </summary>
    [DataField]
    public float TearChanceOnDamage
    {
        get => _tearChanceOnDamage;
        set => _tearChanceOnDamage = Math.Clamp(value, 0f, 1f);
    }

    private float _tearChanceOnDamage;

    /// <summary>
    /// Время надевания маски при помощи атаки.
    /// </summary>
    [DataField]
    public TimeSpan AttackEquipTime = TimeSpan.FromSeconds(5f);

    #region Restrictions

    /// <summary>
    /// Будет ли маска блокировать возможность вырваться?
    /// </summary>
    [DataField]
    public bool BlockStopPulling = true;

    /// <summary>
    /// Будет ли маска блокировать возможность атаковать?
    /// </summary>
    [DataField]
    public bool BlockAttacks = true;

    #endregion

    #region Safe time

    /// <summary>
    /// Безопасное время, в течение которого запрещено снимать маску.
    /// Устанавливается после надевания маски на объект.
    /// </summary>
    [DataField]
    public TimeSpan SafeTime = TimeSpan.FromMinutes(5f);

    /// <summary>
    /// Время окончания безопасного времени
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? SafeTimeEnd;

    #endregion

    #region Sounds

    /// <summary>
    /// Звук экипировки маски на сущность.
    /// </summary>
    [DataField]
    public SoundSpecifier? EquipSound;

    /// <summary>
    /// Звук успешного разрыва маски сущностью.
    /// </summary>
    [DataField]
    public SoundSpecifier? TearSound;

    // TODO: Постоянный звук разрывания маски, которые будет проигрываться на протяжении всего процесса

    #endregion
}
