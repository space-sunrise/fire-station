using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Маркер маски-изоленты SCP-933 на слоте mask. Спавн с <see cref="AwaitingHostTransformation"/> для самозаражения.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933TapeMaskComponent : Component
{
    /// <summary>
    /// Если true — после таймера носитель станет носителем SCP-933-02 (сам наклеил на себя).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AwaitingHostTransformation;

    /// <summary>
    /// Секунды до «отпадания» и пробуждения хозяина (только при самонаклеивании).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IncubationSeconds = 8f;
}
