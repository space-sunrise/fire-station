using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared._Scp.Scp247;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp247Component : Component
{
    /// <summary>
    /// Время непрерывного наблюдения, после которого наблюдатель видит настоящий облик SCP-247.
    /// </summary>
    [DataField]
    public TimeSpan RequiredWatchTime = TimeSpan.FromSeconds(3); //TimeSpan.FromMinutes(8);

    /// <summary>
    /// Интервал без событий наблюдения, после которого наблюдение считается прерванным.
    /// </summary>
    [DataField]
    public TimeSpan WatchGracePeriod = TimeSpan.FromSeconds(0.6f);

    /// <summary>
    /// Состояние наблюдения SCP-247 за каждым наблюдателем на текущем клиенте или сервере.
    /// </summary>
    [NonSerialized]
    public List<Scp247WatchState> Watchers = new();

    /// <summary>
    /// Показывать ли настоящий облик SCP-247 локальному наблюдателю.
    /// </summary>
    [NonSerialized]
    public bool AngryForLocalViewer;

    /// <summary>
    /// Последнее состояние RSI, применённое локальным клиентом.
    /// </summary>
    [NonSerialized]
    public string? RenderedState;

    /// <summary>
    /// Наблюдатели, которых SCP-247 уже атаковал и которым разрешено отвечать ему.
    /// </summary>
    [AutoNetworkedField]
    public HashSet<NetEntity> AttackedViewers = new();
}

public sealed class Scp247WatchState(EntityUid viewer, TimeSpan startedAt, TimeSpan lastSeen)
{
    public readonly EntityUid Viewer = viewer;
    public TimeSpan StartedAt = startedAt;
    public TimeSpan LastSeen = lastSeen;
    public bool Triggered;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp247ProtectionComponent : Component;
