using Content.Shared._Scp.Other.EmitSoundRandomly;
using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
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

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<Scp096RequireUpdateVisualsEvent>(OnUpdateStateRequest);

        Log.Level = LogLevel.Debug;
    }

    private void OnUpdateStateRequest(Scp096RequireUpdateVisualsEvent args)
    {
        var uid = GetEntity(args.NetEntity);

        if (!TryComp<Scp096Component>(uid, out var scp096Component))
            return;

        UpdateVisualState((uid, scp096Component));
    }

    private void UpdateVisualState(Entity<Scp096Component> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var useDownState = UseDownState(ent);
        var inRage = HasComp<ActiveScp096RageComponent>(ent);
        var isHeatingUp = HasComp<ActiveScp096HeatingUpComponent>(ent);
        var agroToDead = ent.Comp.AgroToDeadAnimation;
        var deadToIdle = ent.Comp.DeadToIdleAnimation;

        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Dead, !agroToDead && useDownState);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Agro, inRage && !useDownState);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Heating, isHeatingUp);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.AgroToDead, agroToDead);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.DeadToIdle, deadToIdle);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Idle, !agroToDead && !deadToIdle && !isHeatingUp && !inRage && !useDownState);

        Log.Verbose($"useDownState = {useDownState}; inRage = {inRage}; isHeatingUp = {isHeatingUp}; agroToDead = {agroToDead}; deadToIdle = {deadToIdle}; ");
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
        UpdateVisualState(ent);
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
}
