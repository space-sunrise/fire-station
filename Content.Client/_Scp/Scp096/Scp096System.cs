using Content.Shared._Scp.Other.EmitSoundRandomly;
using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096System : SharedScp096System
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<Scp096RequireUpdateVisualsEvent>(OnUpdateStateRequest);

        _clyde.OnWindowFocused += OnFocusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _clyde.OnWindowFocused -= OnFocusChanged;
    }

    private void OnUpdateStateRequest(Scp096RequireUpdateVisualsEvent args)
    {
        var uid = GetEntity(args.NetEntity);

        if (!TryComp<Scp096Component>(uid, out var scp096Component))
            return;

        UpdateVisualState((uid, scp096Component));
    }

    // Ебанный предикшен
    // Это существует тут, так как иногда значения установленные из shared кода могут не успеть установиться на клиенте(я хз как так)
    // И происходит миспредикт -> скромник иногда мог ходить с обычным спрайтом в ярости. Поэтому для точности после установки стейта с сервера он еще раз перепроверяет внешку скромника.
    protected override void OnHandleState(Entity<Scp096Component> ent, ref AfterAutoHandleStateEvent args)
    {
        base.OnHandleState(ent, ref args);

        UpdateVisualState(ent);
    }

    private void UpdateVisualState(Entity<Scp096Component> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var useDownState = UseDownState(ent);
        var inRage = HasComp<ActiveScp096RageComponent>(ent);

        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Dead, useDownState);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Idle, !inRage && !useDownState);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Agro, inRage && !useDownState);
    }

    /// <summary>
    /// Проверяет, должен ли скромник находиться в лежачем состоянии.
    /// </summary>
    private bool UseDownState(EntityUid uid)
    {
        return _mob.IsIncapacitated(uid)
               || HasComp<SleepingComponent>(uid)
               || HasComp<StunnedComponent>(uid)
               || HasComp<KnockedDownComponent>(uid)
               || TryComp<StandingStateComponent>(uid, out var standing) && !standing.Standing;
    }

    private void OnPlayerAttached(Entity<Scp096Component> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay(ent);
    }

    private void OnPlayerDetached(Entity<Scp096Component> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    protected override void OnInit(Entity<Scp096Component> ent, ref ComponentInit args)
    {
        base.OnInit(ent, ref args);

        if (_player.LocalEntity != ent)
            return;

        AddOverlay(ent);
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        RemoveOverlay();
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

    private void OnFocusChanged(WindowFocusedEventArgs args)
    {
        if (args.Window != _clyde.MainWindow)
            return;

        if (!_player.LocalEntity.HasValue)
            return;

        var player = _player.LocalEntity.Value;

        if (!TryComp<Scp096Component>(player, out var scp096) || !TryComp<AmbientSoundComponent>(player, out var ambientSound))
            return;

        var ambienceEnabled =
            args.Focused
            || HasComp<ActiveScp096RageComponent>(player)
            || HasComp<ActiveScp096HeatingUpComponent>(player);

        _ambientSound.SetAmbienceWithoutDirty(player, ambienceEnabled, ambientSound);
    }
}
