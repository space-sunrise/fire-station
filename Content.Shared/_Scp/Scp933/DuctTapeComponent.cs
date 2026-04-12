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
}
