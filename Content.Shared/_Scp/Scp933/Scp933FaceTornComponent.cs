using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Игрок по-прежнему управляет персонажем; это только эффект ритуала: лицо снято, немота и т.д. ИИ не вовлечён.
/// <see cref="TornBy"/> — кто завершил срыв ленты, нужен лишь для снятия эффектов при гибели носителя ленты.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933FaceTornComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? TornBy;
}
