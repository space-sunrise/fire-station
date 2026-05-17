using System;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Полоска SCP-933, которую можно наклеить на лицо и позже сорвать.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933TapeMaskComponent : Component
{
    public const float MinimumApplyDelaySeconds = 0.1f;
    public const float MinimumRipDelaySeconds = 0.1f;

    private float _applyDelaySeconds = 2.5f;
    private float _ripDelaySeconds = 2.5f;

    /// <summary>
    /// Время наклейки полоски (do-after) на себя или другого.
    /// Минимальное значение ограничено MinimumApplyDelaySeconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ApplyDelaySeconds
    {
        get => _applyDelaySeconds;
        set => _applyDelaySeconds = MathF.Max(MinimumApplyDelaySeconds, value);
    }

    /// <summary>
    /// Время срыва полоски с лица (do-after).
    /// Минимальное значение ограничено MinimumRipDelaySeconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RipDelaySeconds
    {
        get => _ripDelaySeconds;
        set => _ripDelaySeconds = MathF.Max(MinimumRipDelaySeconds, value);
    }

    /// <summary>
    /// Временный серверный флаг: разрешить снять ленту только в рамках ритуального do-after.
    /// </summary>
    [ViewVariables]
    public bool RitualUnequipAllowed;

    /// <summary>
    /// Кто именно сейчас имеет право снять ленту через ритуальный do-after.
    /// </summary>
    [ViewVariables]
    public EntityUid? RitualUnequipUser;

    /// <summary>
    /// Текущий ритуал может проходить без хоста (одноразовый аварийный срыв).
    /// </summary>
    [ViewVariables]
    public bool RitualAllowNonHost;

    /// <summary>
    /// Одноразовый аварийный срыв без хоста.
    /// После первого успешного применения становится false.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool EmergencyRipAvailable = true;

    /// <summary>
    /// Слоты, в которые можно экипировать маску.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> EquipSlots = new() { "mask" };

    /// <summary>
    /// Прерывать ли do-after при движении пользователя.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnUserMove = true;

    /// <summary>
    /// Прерывать ли do-after при получении урона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDamage = true;

    /// <summary>
    /// Требуется ли свободная рука для do-after.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedHand = true;

    /// <summary>
    /// Звук, когда полоску наклеивают на лицо.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ApplyToFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");

    /// <summary>
    /// Звук, когда полоску срывают с лица.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier RipFromFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/peeloff.ogg");
}
