using System.Numerics;
using Content.Shared._Scp.Scp096;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096Overlay : Overlay
{
    private readonly EntityLookupSystem _entityLookup;
    private readonly Entity<Scp096Component> _scp096;
    private readonly IEyeManager _eyeManager;
    private readonly Font _font;

    public Scp096Overlay(EntityLookupSystem entityLookup, Entity<Scp096Component> scp096, IEyeManager eyeManager)
    {
        _entityLookup = entityLookup;
        _scp096 = scp096;
        _eyeManager = eyeManager;

        var resourceCache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_scp096.Comp.InRageMode)
        {
            return;
        }

        DrawScpTimer(args);
    }

    private void DrawScpTimer(in OverlayDrawArgs args)
    {
        var aabb = _entityLookup.GetWorldAABB(_scp096.Owner);

        var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center +
                                                          new Angle(-_eyeManager.CurrentEye.Rotation).RotateVec(
                                                              aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);

        var remainingTime = _scp096.Comp.RageTime - _scp096.Comp.RageAcc;

        args.ScreenHandle.DrawString(_font, screenCoordinates, $"{remainingTime:F2}");
    }
}
