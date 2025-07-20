using Content.Shared._Scp.Watching.FOV;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.FieldOfView;

public sealed class FieldOfViewOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private FieldOfViewOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new FieldOfViewOverlay();

        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(EntityUid uid, FieldOfViewComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, FieldOfViewComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
