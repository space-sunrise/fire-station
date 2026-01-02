using Content.Shared._Scp.Scp096.Main.Components;
using Robust.Shared.Player;

namespace Content.Client._Scp.Scp096;

public sealed partial class Scp096System
{
    private Scp096RageOverlay? _rageOverlay;

    private void InitializeRage()
    {
        SubscribeLocalEvent<ActiveScp096RageComponent, LocalPlayerAttachedEvent>(OnRagePlayerAttached);
        SubscribeLocalEvent<ActiveScp096RageComponent, LocalPlayerDetachedEvent>(OnRagePlayerDetached);
    }

    protected override void OnRageStart(Entity<ActiveScp096RageComponent> ent, ref ComponentStartup args)
    {
        base.OnRageStart(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        AddRageOverlay();
    }

    protected override void OnRageShutdown(Entity<ActiveScp096RageComponent> ent, ref ComponentShutdown args)
    {
        base.OnRageShutdown(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        RemoveRageOverlay();
    }

    private void OnRagePlayerAttached(Entity<ActiveScp096RageComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddRageOverlay();
    }

    private void OnRagePlayerDetached(Entity<ActiveScp096RageComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveRageOverlay();
    }

    private void AddRageOverlay()
    {
        if (_rageOverlay != null)
            return;

        _rageOverlay = new();
        _overlayMan.AddOverlay(_rageOverlay);
    }

    private void RemoveRageOverlay()
    {
        if (_rageOverlay == null)
            return;

        _overlayMan.RemoveOverlay(_rageOverlay);
        _rageOverlay = null;
    }
}
