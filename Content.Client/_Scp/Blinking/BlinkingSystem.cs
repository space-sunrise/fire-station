using Content.Shared._Scp.Blinking;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private BlinkingOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BlinkableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlinkableComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlinkableComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new BlinkingOverlay();
    }

    private void OnPlayerAttached(EntityUid uid, BlinkableComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, BlinkableComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, BlinkableComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnShutdown(EntityUid uid, BlinkableComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    public override bool CanCloseEyes(EntityUid uid)
    {
        return false;
    }
}
