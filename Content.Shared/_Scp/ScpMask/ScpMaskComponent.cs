using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.ScpMask;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpMaskComponent : Component, IClothingSlots
{
    /// <summary>
    /// Вайтлист таргетов, на которых можно надеть маску
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public EntityWhitelist TargetWhitelist = default!;

    /// <summary>
    /// Слот, в котором должна находиться маска
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public SlotFlags Slots { get; set; } = SlotFlags.MASK;

    /// <summary>
    /// Время, которое сцп 096 должен рвать свою маску
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public float TearTime = 10f; // 10 секунд

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

    #region Safe time

    [DataField, AlwaysPushInheritance]
    public float SafeTime = 300; // 5 минут

    [AutoNetworkedField, AlwaysPushInheritance]
    public TimeSpan? SafeTimeEnd;

    #endregion

    #region Sounds

    [DataField, AlwaysPushInheritance]
    public SoundSpecifier? EquipSound;

    [DataField, AlwaysPushInheritance]
    public SoundSpecifier? TearSound;

    // TODO: Постоянный звук разрывания маски, которые будет проигрываться на протяжении всего процесса

    #endregion

}
