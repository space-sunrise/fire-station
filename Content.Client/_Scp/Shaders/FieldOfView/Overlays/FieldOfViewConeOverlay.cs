using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.FieldOfView.Overlays;

/// <summary>
///     Renders the actual "cone" part of the viewcone, no alpha modulation
/// </summary>
public sealed class FieldOfViewConeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly FieldOfViewOverlayManagementSystem _fovManagement;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private static readonly ProtoId<ShaderPrototype> ShaderPrototype = "Viewcone";
    private readonly ShaderInstance _shader;

    private float _coneAngle;
    private float _coneFeather;
    private float _coneIgnoreRadius;
    private float _coneIgnoreFeather;

    public FieldOfViewConeOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _proto.Index(ShaderPrototype).InstanceUnique();
        _fovManagement = _ent.System<FieldOfViewOverlayManagementSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_fovManagement.PlayerEntity.HasValue)
            return false;

        var player = _fovManagement.PlayerEntity.Value;

        if (args.Viewport.Eye != player.Comp1.Eye)
            return false;

        _coneAngle = player.Comp2.Angle;
        _coneFeather = player.Comp2.AngleTolerance;
        _coneIgnoreRadius = (player.Comp2.ConeIgnoreRadius - player.Comp2.ConeIgnoreFeather) * 50f;
        _coneIgnoreFeather = Math.Max(player.Comp2.ConeIgnoreFeather * 200f, 8f);

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || !_fovManagement.PlayerEntity.HasValue)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Zoom", _fovManagement.PlayerEntity.Value.Comp1.Zoom.X);
        _shader.SetParameter("ViewAngle", (float) _fovManagement.PlayerEntity.Value.Comp2.CurrentAngle.Theta);
        _shader.SetParameter("ConeAngle", _coneAngle);
        _shader.SetParameter("ConeFeather", _coneFeather);
        _shader.SetParameter("ConeIgnoreRadius", _coneIgnoreRadius);
        _shader.SetParameter("ConeIgnoreFeather", _coneIgnoreFeather);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
