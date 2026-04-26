using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Other.ScpOnSoundVisibility;

public sealed class ScpOnSoundVisibilityResetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly ScpOnSoundVisibilityHudSystem _hud;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ScpOnSoundVisibilityResetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _hud = _ent.System<ScpOnSoundVisibilityHudSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _hud.CachedBaseAlphas.Count > 0 && _hud.CanDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _hud.RestoreCachedBaseAlphas();
    }
}
