using Content.Shared._Scp.Blinking;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private readonly SoundSpecifier _blinkingStartSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/start.ogg");
    private readonly SoundSpecifier _blinkingEndSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/end.ogg");

    private readonly SoundSpecifier _blinkSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/blink.ogg");

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

    protected override void PlayBlinkSound(EntityUid uid)
    {
        if (_player.LocalEntity != uid)
            return;

        _audio.PlayGlobal(_blinkSound, uid);
    }
}
