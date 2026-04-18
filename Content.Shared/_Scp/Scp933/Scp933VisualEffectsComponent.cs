using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Визуальные эффекты для SCP-933.
/// Настройки визуальных эффектов вынесены в компонент.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933VisualEffectsComponent : Component
{
    /// <summary>
    /// Скрывать ли глаза при наложении ленты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HideEyes = true;

    /// <summary>
    /// Скрывать ли морду при наложении ленты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HideSnout = true;

    /// <summary>
    /// Дополнительные слои для скрытия (настройка через YAML).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<HumanoidVisualLayers>? AdditionalHiddenLayers = null;

    /// <summary>
    /// Цвет оверлея при инкубации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color IncubationOverlayColor = new(0.5f, 0.5f, 0.5f, 0.3f);

    /// <summary>
    /// Длительность анимации срыва (в секундах).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RipAnimationDuration = 0.5f;

    /// <summary>
    /// Длительность анимации появления хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HostEmergeAnimationDuration = 1.0f;
}
