using System.Numerics;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Maths;

namespace Content.Client._Scp.Shaders.FieldOfView;

public sealed class FieldOfViewOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _spriteSystem;

    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _blurXShader;
    private readonly ShaderInstance _blurYShader;

    private IRenderTexture? _blurPass;
    private IRenderTexture? _backBuffer;

    private Angle _currentAngle;
    private const float LerpSpeed = 8f;
    private const float ConeOpacity = 0.85f;
    private const float EdgeHardness = 0.08f;
    private const float SafeZoneEdgeWidth = 24.0f;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FieldOfViewOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        _shader = _prototype.Index<ShaderPrototype>("FieldOfView").InstanceUnique();
        _blurXShader = _prototype.Index<ShaderPrototype>("BlurryVisionX").InstanceUnique();
        _blurYShader = _prototype.Index<ShaderPrototype>("BlurryVisionY").InstanceUnique();
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        _blurPass?.Dispose();
        _backBuffer?.Dispose();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _player.LocalEntity;

        if (!_entManager.HasComponent<FieldOfViewComponent>(playerEntity))
            return false;

        if (!_entManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComp) || args.Viewport.Eye != eyeComp.Eye)
            return false;

        var size = args.Viewport.Size;
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
        var playerEntity = _player.LocalEntity;
        var eye = args.Viewport.Eye;

        if (ScreenTexture == null || _backBuffer == null || _blurPass == null || eye == null ||
            !_entManager.TryGetComponent(playerEntity, out TransformComponent? playerXform) ||
            !_entManager.TryGetComponent(playerEntity, out FieldOfViewComponent? visionComponent) ||
            !_entManager.TryGetComponent(playerEntity, out SpriteComponent? sprite))
        {
            return;
        }

        var handle = args.WorldHandle;
        var viewportBounds = new Box2(Vector2.Zero, args.Viewport.Size);

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

        var correctedAngle = playerXform.LocalRotation - Angle.FromDegrees(90);

        if (_currentAngle.Theta == 0)
            _currentAngle = correctedAngle;

        var targetAngle = correctedAngle.GetDir().ToAngle();

        _currentAngle = Angle.Lerp(_currentAngle, targetAngle, LerpSpeed * (float)_timing.FrameTime.TotalSeconds);
        var forwardVec = _currentAngle.ToVec();

        var worldPos = _transform.GetWorldPosition(playerXform);
        var screenPos = args.Viewport.WorldToLocal(worldPos);
        screenPos.Y = args.Viewport.Size.Y - screenPos.Y;

        var fovCosine = FieldOfViewSystem.GetFovCosine(visionComponent.Angle);

        var bounds = _spriteSystem.GetLocalBounds((playerEntity.Value, sprite));
        var worldRadius = bounds.Height;

        var zoom = eye.Zoom.X;
        var pixelRadius = worldRadius * EyeManager.PixelsPerMeter / zoom;
        var pixelEdgeWidth = SafeZoneEdgeWidth / zoom;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("BLURRED_TEXTURE", _backBuffer.Texture);
        _shader.SetParameter("playerScreenPos", screenPos);
        _shader.SetParameter("playerForward", forwardVec);
        _shader.SetParameter("fovCosine", fovCosine);
        _shader.SetParameter("safeZoneRadius", pixelRadius);
        _shader.SetParameter("coneOpacity", ConeOpacity);
        _shader.SetParameter("edgeHardness", EdgeHardness);
        _shader.SetParameter("safeZoneEdgeWidth", pixelEdgeWidth);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
