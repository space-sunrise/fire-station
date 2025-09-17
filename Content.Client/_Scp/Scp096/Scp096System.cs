using Content.Shared._Scp.Scp096;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096System : SharedScp096System
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private Scp096Overlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<Scp096RageEvent>(OnRage);
        SubscribeLocalEvent<Scp096Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp096Component, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnRage(Scp096RageEvent args)
    {
        // Здесь происходит ваще хз че, поэтому распишу для понятности

        // Так как ивент вызывающий этот метод вызывается каждый раз, когда 096 видит новый таргет
        // То нужно очищать оверлей, чтобы клиенту не засрать 999 оверлеями лишними
        RemoveOverlay();

        // Если мы не в рейдж моде, то мы вышли из него
        // Значит все нужные действия (убрать оверлей) мы уже сделали и можно выходить с метода
        if (!args.InRage)
            return;

        // Если игрок это не 096, то ему не нужен оверлей
        if (!TryComp<Scp096Component>(_player.LocalEntity, out var scp096Component))
            return;

        _overlay = new(_transform, scp096Component.Targets);

        _overlayMan.AddOverlay(_overlay);
    }

    /// <summary>
    /// Сделал так же этот метод добавочно к OnRage, чтобы улучшить выдачу оверлея, когда агр начался без игрока в ентити скромника
    /// </summary>
    private void OnPlayerAttached(EntityUid uid, Scp096Component component, LocalPlayerAttachedEvent args)
    {
        if (_overlay != null)
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
    }
}
