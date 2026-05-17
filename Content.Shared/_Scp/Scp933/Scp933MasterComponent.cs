using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Носитель ленты после инкубации. Обычный игрок с усилениями и ритуалом ленты.
/// Настройки хранятся в Scp933RitualSettingsComponent.
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

    /// <summary>
    /// Время появления хоста.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan HostEmergedAt;

    /// <summary>
    /// Количество жертв на момент появления.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int VictimCountAtEmerge;
}
