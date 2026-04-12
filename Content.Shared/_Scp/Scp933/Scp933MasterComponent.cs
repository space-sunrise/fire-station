using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент контроллера для SCP-933-02 - портабщика ленты.
/// Управляет контролируемыми жертвами и ленте.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933MasterComponent : Component
{
    /// <summary>
    /// Лента которую держит SCP-933-02.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? TapeEntity;

    /// <summary>
    /// Контролируемые жертвы.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Controlled = new();

    /// <summary>
    /// Максимальное количество одновременно контролируемых жертв.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxControlled = 10;

    /// <summary>
    /// Базовая цель для преследования (если есть).
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentTarget;

    /// <summary>
    /// Размер площади видимости (в метрах).
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float VisionRange = 15f;

    /// <summary>
    /// Расстояние от которого можно луч видимости (метров).
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LightBlindRange = 1f;

    /// <summary>
    /// Яркость света которая ослабляет видимость (люмены).
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LightThreshold = 100f;
}
