using Content.Shared._Scp.Other.EmitSoundRandomly;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp096;

// TODO: Управление лицом скромника в статус эффектами, смена спрайтов при смене состояния.
// Что-то типо интерфейса дума с лицом думгая, но для скромника
public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        InitializeWidget();

        Log.Level = LogLevel.Debug;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ShutdownWidget();
    }

    private void OnPlayerAttached(Entity<Scp096Component> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay(ent);
        EnsureWidgetExist();
    }

    private void OnPlayerDetached(Entity<Scp096Component> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
        RemoveWidget();
    }

    protected override void OnInit(Entity<Scp096Component> ent, ref ComponentInit args)
    {
        base.OnInit(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        EnsureWidgetExist();
        AddOverlay(ent);
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        RemoveOverlay();
        RemoveWidget();
    }

    private void AddOverlay(Entity<Scp096Component> ent)
    {
        if (_overlay != null)
            return;

        _overlay = new(ent);
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }

    protected override void OnEmitSoundRandomly(Entity<Scp096Component> ent, ref BeforeRandomlyEmittingSoundEvent args)
    {
        base.OnEmitSoundRandomly(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        if (!_clyde.IsFocused)
            args.Cancel();
    }
}
