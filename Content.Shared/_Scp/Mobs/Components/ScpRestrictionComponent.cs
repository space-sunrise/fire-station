using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Mobs.Components;

/// <summary>
/// Отключает некоторые взаимодействия для владельца компонента. Полезно для сцп
/// // TODO: Сделать отключенные взаимодействие конфигурируемыми через какие-нибудь boolы
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScpRestrictionComponent : Component
{

}
