using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Grain;

public sealed class GrainOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private GrainOverlay _grainOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _grainOverlay = new ();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        AddGrainOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveGrainOverlay();
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
