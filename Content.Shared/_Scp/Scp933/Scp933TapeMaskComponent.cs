using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Полоска SCP-933, которую можно наклеить на лицо и позже сорвать.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933TapeMaskComponent : Component
{
    /// <summary>
    /// Время наклейки полоски (do-after) на себя или другого.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ApplyDelaySeconds = 2.5f;

    /// <summary>
    /// Время срыва полоски с лица (do-after).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RipDelaySeconds = 2.5f;

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
    [DataField]
    public bool EmergencyRipAvailable = true;

    /// <summary>
    /// Звук, когда полоску наклеивают на лицо.
    /// </summary>
    [DataField]
    public SoundSpecifier ApplyToFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");

    /// <summary>
    /// Звук, когда полоску срывают с лица.
    /// </summary>
    [DataField]
    public SoundSpecifier RipFromFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/peeloff.ogg");
}
