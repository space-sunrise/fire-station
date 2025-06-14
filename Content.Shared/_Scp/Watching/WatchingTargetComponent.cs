using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching;

/// <summary>
/// Компонент-маркер, который позволяет системе смотрения включить владельца в обработку
/// Это позволит вызывать ивенты на владельце, когда на него кто-то посмотрит
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WatchingTargetComponent : Component
{
    /// <summary>
    /// Словарь всех сущностей, что уже видел цель.
    /// Сохраняет время последнего взгляда
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<NetEntity, TimeSpan> AlreadyLookedAt = new();

    public TimeSpan NextTimeWatchedCheck;
}
