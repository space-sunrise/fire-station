using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Shaders.FieldOfView.Overlays;

/// <summary>
/// Рисует конус видимости и небольшой круг вокруг персонажа
/// </summary>
public sealed class FieldOfViewConeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly FieldOfViewOverlayManagementSystem _fovManagement;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _blurXShader;
    private readonly ShaderInstance _blurYShader;

    private static readonly ProtoId<ShaderPrototype> ViewconeShaderProtoId = "Viewcone";
    private static readonly ProtoId<ShaderPrototype> BlurryXShaderProtoId = "BlurryVisionX";
    private static readonly ProtoId<ShaderPrototype> BlurryYShaderProtoId = "BlurryVisionY";

    private IRenderTexture? _blurPass;
    private IRenderTexture? _backBuffer;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Размер текстуры размытия.
    /// </summary>
    public float BlurScale = 0.7f;

    /// <summary>
    /// Прозрачность конуса
    /// </summary>
    public float Opacity = 0.85f;

    public FieldOfViewConeOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _proto.Index(ViewconeShaderProtoId).InstanceUnique();
        _blurXShader = _proto.Index(BlurryXShaderProtoId).InstanceUnique();
        _blurYShader = _proto.Index(BlurryYShaderProtoId).InstanceUnique();

        _fovManagement = _ent.System<FieldOfViewOverlayManagementSystem>();
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _shader.Dispose();
        _blurXShader.Dispose();
        _blurYShader.Dispose();

        _blurPass?.Dispose();
        _backBuffer?.Dispose();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_fovManagement.PlayerEntity.HasValue)
            return false;

        var player = _fovManagement.PlayerEntity.Value;

        if (args.Viewport.Eye != player.Comp1.Eye)
            return false;

        var size = (Vector2i)(args.Viewport.Size * BlurScale);
        if (_backBuffer == null || _backBuffer.Size != size)
        {
            _backBuffer?.Dispose();
            _backBuffer = _clyde.CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "fov-backbuffer");

            _blurPass?.Dispose();
            _blurPass = _clyde.CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "fov-blurpass");
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _backBuffer == null || _blurPass == null || !_fovManagement.PlayerEntity.HasValue)
            return;

        var (_, eye, fov, _) = _fovManagement.PlayerEntity.Value;

        var handle = args.WorldHandle;
        var viewportBounds = new Box2(Vector2.Zero, _blurPass.Size);

        if (_timing.CurTime >= _nextUpdate)
        {
            handle.RenderInRenderTarget(_blurPass, () =>
            {
                _blurXShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                handle.UseShader(_blurXShader);
                handle.DrawRect(viewportBounds, Color.White);
            }, Color.Transparent);

            handle.RenderInRenderTarget(_backBuffer, () =>
            {
                _blurYShader.SetParameter("SCREEN_TEXTURE", _blurPass.Texture);
                handle.UseShader(_blurYShader);
                handle.DrawRect(viewportBounds, Color.White);
            }, Color.Transparent);

            _nextUpdate = _timing.CurTime + _fovManagement.UpdateInterval;
        }

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("BLURRED_TEXTURE", _backBuffer.Texture);
        _shader.SetParameter("coneOpacity", Opacity);

        _shader.SetParameter("Zoom", eye.Zoom.X);
        _shader.SetParameter("ViewAngle", (float) fov.CurrentAngle.Theta);
        _shader.SetParameter("ConeAngle", fov.Angle);
        _shader.SetParameter("ConeFeather", fov.AngleFeather);
        _shader.SetParameter("ConeIgnoreRadius", (fov.ConeIgnoreRadius - fov.ConeIgnoreFeather) * 50f);
        _shader.SetParameter("ConeIgnoreFeather", Math.Max(fov.ConeIgnoreFeather * 200f, 8f));

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
