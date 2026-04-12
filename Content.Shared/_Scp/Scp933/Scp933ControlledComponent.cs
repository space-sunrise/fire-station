using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент для сущностей контролируемых SCP-933-02.
/// Жертвы ленты становятся контролируемы и стремятся к мастеру.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933ControlledComponent : Component
{
    /// <summary>
    /// Сущность SCP-933-02 которая контролирует эту жертву.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Master;

    /// <summary>
    /// Скорость движения контролируемой жертвы.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeed = 5f;
}
