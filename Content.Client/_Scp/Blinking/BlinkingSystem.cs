using Content.Shared._Scp.Blinking;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier EyeOpenSound = new SoundCollectionSpecifier("EyeOpen");
    private static readonly SoundSpecifier EyeCloseSound = new SoundCollectionSpecifier("EyeClose");

    private static readonly SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/blink.ogg");

    private BlinkingOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<BlinkableComponent, LocalPlayerDetachedEvent>(OnDetached);

        _overlay = new BlinkingOverlay();
    }

    protected override void OnOpenedEyes(Entity<BlinkableComponent> ent, ref EntityOpenedEyesEvent args)
    {
        base.OnOpenedEyes(ent, ref args);

        OpenEyes(ent, args.Manual);
    }

    protected override void OnClosedEyes(Entity<BlinkableComponent> ent, ref EntityClosedEyesEvent args)
    {
        base.OnClosedEyes(ent, ref args);

        CloseEyes(ent, args.Manual);
    }

    private void OnAttached(Entity<BlinkableComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnDetached(Entity<BlinkableComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (!_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OpenEyes(EntityUid ent, bool manual)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity != ent)
            return;

        if (!manual && !IsScpNearby(ent))
            return;

        Logger.Debug($"Opened, {_timing.IsFirstTimePredicted}, {_timing.CurTick}");

        _overlay.OpenEyes();
        _audio.PlayGlobal(EyeOpenSound, ent);
    }

    private void CloseEyes(EntityUid ent, bool manual)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity != ent)
            return;

        if (!manual && !IsScpNearby(ent))
            return;

        Logger.Debug($"Closed, {_timing.IsFirstTimePredicted}, {_timing.CurTick}");

        _overlay.CloseEyes();
        _audio.PlayGlobal(EyeCloseSound, ent);
    }
}
