using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Носитель ленты после инкубации. Не «контроллер ИИ» — обычный игрок с усилениями и ритуалом ленты.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933MasterComponent : Component
{
    /// <summary>
    /// Кому уже сорвали лицо после финального срыва ленты (игроки сами ими играют).
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> FaceTornVictims = new();
}
