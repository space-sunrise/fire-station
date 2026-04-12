using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching;

/// <summary>
/// Компонент-маркер, который позволяет системе смотрения включить владельца в обработку
/// Это позволит вызывать ивенты на владельце, когда на него кто-то посмотрит
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class WatchingTargetComponent : Component
{
    /// <summary>
    /// Словарь всех сущностей, что уже видел цель.
    /// Сохраняет время последнего взгляда
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<NetEntity, TimeSpan> AlreadyLookedAt = new();

    /// <summary>
    /// Время между проверками зрения
    /// </summary>
    [DataField]
    public TimeSpan WatchingCheckInterval = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// Время следующей проверки зрения
    /// </summary>
    [AutoNetworkedField, AutoPausedField, ViewVariables]
    public TimeSpan? NextTimeWatchedCheck;

    /// <summary>
    /// Будет ли система обрабатывать только простые ивенты, исключая комплексные проверки и ивенты после них?
    /// </summary>
    /// <remarks>
    /// Используется, когда другие системы вручную обрабатывают данные, чтобы исключить двойную работу
    /// </remarks>
    [DataField]
    public bool SimpleMode;
}
