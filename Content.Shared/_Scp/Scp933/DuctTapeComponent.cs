using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент для ленты SCP-933.
/// Хранит количество использований.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DuctTapeComponent : Component
{
    /// <summary>
    /// Сколько раз еще можно использовать ленту.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UseCount = 1;

    /// <summary>
    /// Время отрыва одной полоски от рулона.
    /// </summary>
    [DataField]
    public float PeelDelaySeconds = 3.5f;

    /// <summary>
    /// Звук, когда отрывают полоску от рулона.
    /// </summary>
    [DataField]
    public SoundSpecifier PullFromRollSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");
}
