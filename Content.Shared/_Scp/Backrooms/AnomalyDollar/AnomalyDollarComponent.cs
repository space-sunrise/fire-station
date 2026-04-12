

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Backrooms.AnomalyDollar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnomalyDollarComponent : Component
{
    /// <summary>
    /// Частота создания копии банкноты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CloneDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Шанс клонирования купюры.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CloneChance = 0.4f;

    /// <summary>
    /// Импульс отброса банкноты в сторону.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ImpulseStrength = 100.0f;

    /// <summary>
    /// Лимит копий по всему миру для этой купюры.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CopiesLimit = 10;

    [DataField, AutoNetworkedField]
    public EntProtoId CloneProto = "ScpAnomalyDollar";

    /// <summary>
    /// Следующее время создания копии банкноты.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextCloneTime;
}
