using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Grain;

public sealed class GrainOverlaySystemSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private GrainOverlay _grainOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _grainOverlay = new ();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (args.Player != _player.LocalSession)
            return;

        _overlayManager.AddOverlay(_grainOverlay);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        if (args.Player != _player.LocalSession)
            return;

        _overlayManager.RemoveOverlay(_grainOverlay);
    }

    #region Pulic API

    public void ToggleGrainOverlay()
    {
        if (_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.RemoveOverlay(_grainOverlay);
        else
            _overlayManager.AddOverlay(_grainOverlay);
    }

    public void AddGrainOverlay()
    {
        if (!_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.AddOverlay(_grainOverlay);
    }

    public void RemoveGrainOverlay()
    {
        if (_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.AddOverlay(_grainOverlay);
    }

    #endregion
}
