using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp173;
using Content.Shared._Scp.Watching;
using Content.Shared.Mind.Components;
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

    private static readonly SoundSpecifier BlinkingStartSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/start.ogg");
    private static readonly SoundSpecifier BlinkingEndSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/end.ogg");

    private static readonly SoundSpecifier EyeOpenSound = new SoundCollectionSpecifier("EyeOpen");
    private static readonly SoundSpecifier EyeCloseSound = new SoundCollectionSpecifier("EyeClose");

    private static readonly SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/blink.ogg");

    private BlinkingOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Приходится использовать MindContainerComponent, потому что в шареде уже запривачено EntityOpenedEyesEvent для Blinkable
        // И самый подходящий вариант подписки на включение/выключение оверлея моргания игроку это MindComponent
        SubscribeLocalEvent<MindContainerComponent, EntityOpenedEyesEvent>(OnEntityOpenedEyes);
        SubscribeLocalEvent<MindContainerComponent, EntityClosedEyesEvent>(OnEntityClosedEyes);

        SubscribeLocalEvent<BlinkableComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<BlinkableComponent, LocalPlayerDetachedEvent>(OnDetached);

        SubscribeLocalEvent<Scp173Component, SimpleEntitySeenEvent>(OnScp173Seen);

        _overlay = new BlinkingOverlay();
    }

    private void OnEntityOpenedEyes(Entity<MindContainerComponent> ent, ref EntityOpenedEyesEvent args)
    {
        if (_player.LocalEntity != ent)
            return;

        if (!_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _audio.PlayGlobal(EyeOpenSound, ent);
    }

    private void OnEntityClosedEyes(Entity<MindContainerComponent> ent, ref EntityClosedEyesEvent args)
    {
        if (_player.LocalEntity != ent)
            return;

        if (!args.Manual && !IsScpNearby(ent))
            return;

        if (_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.AddOverlay(_overlay);
        _audio.PlayGlobal(EyeCloseSound, ent);
    }

    private void OnAttached(Entity<BlinkableComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        TryAddOverlay(ent);
    }

    private void OnDetached(Entity<BlinkableComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (!_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnScp173Seen(Entity<Scp173Component> ent, ref SimpleEntitySeenEvent args)
    {
        if (_player.LocalEntity != args.Viewer)
            return;

        TryAddOverlay(args.Viewer);
    }

    private bool TryAddOverlay(EntityUid ent)
    {
        if (!AreEyesClosed(ent))
            return false;

        if (_overlayMan.HasOverlay<BlinkingOverlay>())
            return false;

        _overlayMan.AddOverlay(_overlay);

        return true;
    }
}
