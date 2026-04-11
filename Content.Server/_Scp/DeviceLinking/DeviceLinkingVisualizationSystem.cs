using Content.Server.Administration;
using Content.Shared._Scp.DeviceLinking;
using Content.Shared.Administration;
using Content.Shared.DeviceLinking;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Scp.DeviceLinking;

public sealed class DeviceLinkingVisualizationSystem : EntitySystem
{
    private TimeSpan _nextOverlayUpdate = TimeSpan.Zero;
    private TimeSpan _overlayUpdateInterval = TimeSpan.FromSeconds(1);

    private readonly HashSet<ICommonSession> _debugSessions = new();

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_debugSessions.Count == 0 || _timing.CurTime < _nextOverlayUpdate)
            return;

        _nextOverlayUpdate = _timing.CurTime + _overlayUpdateInterval;

        UpdateOverlay();
    }

    /// <summary>
    ///     Переключает отображение подключённых сетей.
    /// </summary>
    public void ToggleDebugView(ICommonSession session)
    {
        bool isEnabled;
        if (_debugSessions.Add(session))
            isEnabled = true;
        else
        {
            _debugSessions.Remove(session);
            isEnabled = false;
        }

        var ev = new DeviceLinkOverlayToggledEvent(isEnabled);
        RaiseNetworkEvent(ev, session.Channel);
    }

    private void UpdateOverlay()
    {
        if (_debugSessions.Count == 0)
            return;

        List<DebugEntityConnectionData> rays = new();

        var query = EntityQueryEnumerator<DeviceLinkSourceComponent>();
        while (query.MoveNext(out var uid, out var source))
        {
            if (source.LinkedPorts.Count == 0)
                continue;

            var netUid = GetNetEntity(uid);
            List<NetEntity> entities = new();

            foreach (var output in source.LinkedPorts)
                entities.Add(GetNetEntity(output.Key));

            rays.Add(new DebugEntityConnectionData(netUid, entities));
        }

        foreach (var session in _debugSessions)
            RaiseNetworkEvent(new DeviceLinkOverlayData(rays), session);
    }
}

/// <summary>
///     Переключает отображение связку подключенных устройств.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class ShowDeviceLinkCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DeviceLinkingVisualizationSystem _deviceLinking = default!;

    public override string Command => "showdevicelink";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var session = shell.Player;
        if (session == null)
            return;

        _deviceLinking.ToggleDebugView(session);
    }
}
