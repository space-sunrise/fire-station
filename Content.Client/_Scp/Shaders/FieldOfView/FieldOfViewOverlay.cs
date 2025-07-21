using System.Numerics;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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
    public float ConeOpacity;
    private const float EdgeHardness = 0.4f;
    private const float SafeZoneEdgeWidth = 24.0f;

    private readonly EntityQuery<FieldOfViewComponent> _fovQuery;
    private readonly EntityQuery<EyeComponent> _eyeQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    private TransformComponent? _xform;
    private SpriteComponent? _sprite;
    private FieldOfViewComponent? _fov;

    public EntityUid? EntityOverride;

    private static readonly Angle AngleCorrection = Angle.FromDegrees(180);
    private const float DeltaTimeFrameRate = 1f / 30f;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FieldOfViewOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        _fovQuery = _entManager.GetEntityQuery<FieldOfViewComponent>();
        _eyeQuery = _entManager.GetEntityQuery<EyeComponent>();
        _spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        _shader = _prototype.Index<ShaderPrototype>("FieldOfView").InstanceUnique();
        _blurXShader = _prototype.Index<ShaderPrototype>("BlurryVisionX").InstanceUnique();
        _blurYShader = _prototype.Index<ShaderPrototype>("BlurryVisionY").InstanceUnique();
    }

    public void NullifyComponents()
    {
        _xform = null;
        _sprite = null;
        _fov = null;
        EntityOverride = null;
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

        if (!_fovQuery.HasComponent(playerEntity))
            return false;

        if (!_eyeQuery.TryGetComponent(playerEntity, out var eyeComp) || args.Viewport.Eye != eyeComp.Eye)
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
        var chosenEntity = EntityOverride ?? playerEntity;

        if (!chosenEntity.HasValue)
            return;

        var eye = args.Viewport.Eye;
        if (ScreenTexture == null || _backBuffer == null || _blurPass == null || eye == null)
            return;

        if (_xform == null && !_xformQuery.TryGetComponent(chosenEntity, out _xform))
            return;

        if (_fov == null && !_fovQuery.TryGetComponent(playerEntity, out _fov))
            return;

        if (_sprite == null && !_spriteQuery.TryGetComponent(chosenEntity, out _sprite))
            return;

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

        var onScreenAngle = _xform.LocalRotation + eye.Rotation.RoundToCardinalAngle();
        var correctedAngle = onScreenAngle - AngleCorrection;

        if (_currentAngle.Theta == 0)
            _currentAngle = correctedAngle;

        var deltaTime = Math.Min((float)_timing.FrameTime.TotalSeconds, DeltaTimeFrameRate);
        _currentAngle = Angle.Lerp(_currentAngle, correctedAngle, LerpSpeed * deltaTime);

        var forwardVec = _currentAngle.ToVec();

        var worldPos = _transform.GetWorldPosition(_xform);
        var screenPos = args.Viewport.WorldToLocal(worldPos);
        screenPos.Y = args.Viewport.Size.Y - screenPos.Y;

        var fovCosine = FieldOfViewSystem.GetFovCosine(_fov.Angle);

        var bounds = _spriteSystem.GetLocalBounds((chosenEntity.Value, _sprite));
        var worldRadius = Math.Max(bounds.Width, bounds.Height);

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
