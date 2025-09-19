using Content.Shared._Scp.Scp096;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<Scp096Component, Scp096RequireUpdateVisualsEvent>(OnUpdateStateRequest);
    }

    private void OnUpdateStateRequest(Entity<Scp096Component> ent, ref Scp096RequireUpdateVisualsEvent args)
    {
        UpdateVisualState(ent);
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

        var isDead = _mob.IsIncapacitated(ent) || HasComp<SleepingComponent>(ent);

        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Dead, isDead);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Idle, !ent.Comp.InRageMode && !isDead);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Agro, ent.Comp.InRageMode && !isDead);
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

        _overlay = new(ent, _transform);
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
