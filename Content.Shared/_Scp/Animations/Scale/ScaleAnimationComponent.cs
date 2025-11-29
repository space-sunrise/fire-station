using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Animations.Scale;

/// <summary>
/// Компонент, позволяющий проигрывать анимацию "появления сущности" через увеличение размера спрайта.
/// Анимация начинается при появлении и постепенно увеличивает спрайт с <see cref="InitialSize"/> до стандартного размера.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScaleAnimationComponent : Component
{
    /// <summary>
    /// Ключ для определения анимации
    /// </summary>
    [DataField(required: true)]
    public string AnimationKey = "change_this_in_prototype";

    /// <summary>
    /// Время, за которое будет растекаться сущность
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Изначальный размер сущности, из которого она будет растекаться до стандартного
    /// </summary>
    [DataField]
    public Vector2 InitialSize = new (0.25f, 0.25f);

    /// <summary>
    /// Время, к которому анимация закончится.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan? AnimationEndTime;
}
