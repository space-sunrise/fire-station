using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Аудио настройки для SCP-933.
/// Все звуки вынесены в отдельный компонент для кастомизации через YAML.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933AudioComponent : Component
{
    /// <summary>
    /// Звук отрыва полоски от рулона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier PeelSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");

    /// <summary>
    /// Звук наклейки на лицо.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ApplySound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");

    /// <summary>
    /// Звук срыва с лица.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier RipSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/peeloff.ogg");

    /// <summary>
    /// Звук при появлении хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? HostEmergedSound;

    /// <summary>
    /// Звук при срыве лица жертвы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? FaceTornSound;

    /// <summary>
    /// Громкость звуков (0-1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Volume = 1.0f;

    /// <summary>
    /// Радиус слышимости звуков.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AudibleRange = 7.0f;
}
