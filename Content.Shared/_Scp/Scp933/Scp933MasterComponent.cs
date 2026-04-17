using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Носитель ленты после инкубации. Не «контроллер ИИ» — обычный игрок с усилениями и ритуалом ленты.
/// Использует отдельные компоненты для настроек: Scp933RitualSettingsComponent, Scp933VisualEffectsComponent, etc.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933MasterComponent : Component
{
    /// <summary>
    /// Кому уже сорвали лицо после финального срыва ленты (игроки сами ими играют).
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables]
    public HashSet<EntityUid> FaceTornVictims = new();

    /// <summary>
    /// Время появления хоста.
    /// </summary>
    [DataField]
    public TimeSpan HostEmergedAt;

    /// <summary>
    /// Количество жертв на момент появления.
    /// </summary>
    [DataField]
    public int VictimCountAtEmerge;
}
