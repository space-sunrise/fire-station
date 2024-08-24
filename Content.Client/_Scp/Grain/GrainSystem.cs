using Robust.Client.Graphics;

namespace Content.Client._Scp.Grain;

public sealed class GrainSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayManager.AddOverlay(new GrainOverlay());
    }
}
