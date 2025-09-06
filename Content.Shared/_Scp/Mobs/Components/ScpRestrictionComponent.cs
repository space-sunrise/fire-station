using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Mobs.Components;

/// <summary>
/// Отключает некоторые взаимодействия для владельца компонента. Полезно для сцп
/// </summary>
/// <remarks>
/// TODO: Сделать отключенные взаимодействие конфигурируемыми через какие-нибудь boolы
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanPull;

    [DataField, AutoNetworkedField]
    public bool CanBePulled;

    [DataField, AutoNetworkedField]
    public bool CanTakeStaminaDamage;

    [DataField, AutoNetworkedField]
    public bool CanMobCollide;
}
