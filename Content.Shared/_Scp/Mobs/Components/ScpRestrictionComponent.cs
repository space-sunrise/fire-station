using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Mobs.Components;

/// <summary>
/// Отключает некоторые взаимодействия для владельца компонента. Полезно для сцп
/// </summary>
/// <remarks>
/// TODO: Сделать отключенные взаимодействие полностью конфигурируемыми bool
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanBeDisarmed;

    [DataField, AutoNetworkedField]
    public bool CanStandingState;

    [DataField, AutoNetworkedField]
    public bool CanPull;

    [DataField, AutoNetworkedField]
    public bool CanBePulled;

    [DataField, AutoNetworkedField]
    public bool CanTakeStaminaDamage;

    [DataField, AutoNetworkedField]
    public bool CanMobCollide;

    [DataField, AutoNetworkedField]
    public bool CanCarry;
}
