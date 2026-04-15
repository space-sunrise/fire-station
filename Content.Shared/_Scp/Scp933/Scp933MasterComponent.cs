using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Носитель ленты после инкубации. Не «контроллер ИИ» — обычный игрок с усилениями и ритуалом ленты.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933MasterComponent : Component
{
    /// <summary>
    /// Разрешать оживление при переходе в состояние хоста.
    /// </summary>
    [DataField]
    public bool AllowRevivesOnHost = true;

    /// <summary>
    /// Исцелять хоста и его жертв при переходе в состояние хоста.
    /// </summary>
    [DataField]
    public bool HealOnHostBecoming = true;

    /// <summary>
    /// Порог живого состояния после ритуала.
    /// </summary>
    [DataField]
    public FixedPoint2 AliveThreshold = FixedPoint2.Zero;

    /// <summary>
    /// Порог критического состояния после ритуала.
    /// </summary>
    [DataField]
    public FixedPoint2 CriticalThreshold = 700;

    /// <summary>
    /// Порог смерти после ритуала.
    /// </summary>
    [DataField]
    public FixedPoint2 DeadThreshold = 800;

    /// <summary>
    /// Урон в ближнем бою после ритуала.
    /// </summary>
    [DataField]
    public FixedPoint2 HostBluntDamage = 25;

    /// <summary>
    /// Кому уже сорвали лицо после финального срыва ленты (игроки сами ими играют).
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> FaceTornVictims = new();
}
