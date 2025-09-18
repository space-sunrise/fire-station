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
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, Scp096RageChangedEvent>(OnRage);
        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<Scp096Component, Scp096RequireUpdateVisualsEvent>(OnUpdateStateRequest);
    }

    private void OnUpdateStateRequest(Entity<Scp096Component> ent, ref Scp096RequireUpdateVisualsEvent args)
    {
        UpdateVisualState(ent);
    }

    // TODO: Починить, что комбат мод блокирует анимации движения
    // TODO: Починить тройное срабатывание
    private void UpdateVisualState(Entity<Scp096Component> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var isDead = _mob.IsIncapacitated(ent) || HasComp<SleepingComponent>(ent);

        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Dead, isDead);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Idle, !ent.Comp.InRageMode && !isDead);
        _sprite.LayerSetVisible(ent.Owner, Scp096VisualsState.Agro, ent.Comp.InRageMode && !isDead);
    }

    private void OnRage(Entity<Scp096Component> ent, ref Scp096RageChangedEvent args)
    {
        UpdateVisualState(ent);

        if (ent != _player.LocalEntity)
            return;

        if (_overlay == null)
            return;

        _overlay.Targets = ent.Comp.Targets;
    }

    /// <summary>
    /// Сделал так же этот метод добавочно к OnRage, чтобы улучшить выдачу оверлея, когда агр начался без игрока в ентити скромника
    /// </summary>
    private void OnPlayerAttached(EntityUid uid, Scp096Component component, LocalPlayerAttachedEvent args)
    {
        if (_overlay == null)
            _overlay = new(_transform);

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, Scp096Component component, LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        RemoveOverlay();
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
