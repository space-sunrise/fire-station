using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.FieldOfView;

public sealed class FieldOfViewOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _spriteSystem;
    private readonly ShaderInstance _shader;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FieldOfViewOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entManager.System<SharedTransformSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();
        _shader = _prototype.Index<ShaderPrototype>("FieldOfView").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _player.LocalEntity;

        if (!_entManager.HasComponent<FieldOfViewComponent>(playerEntity))
            return false;

        if (!_entManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComp) || args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _player.LocalEntity;

        if (ScreenTexture == null ||
            !_entManager.TryGetComponent(playerEntity, out TransformComponent? playerXform) ||
            !_entManager.TryGetComponent(playerEntity, out FieldOfViewComponent? visionComponent) ||
            !_entManager.TryGetComponent(playerEntity, out SpriteComponent? sprite))
        {
            return;
        }

        var worldPos = _transform.GetWorldPosition(playerXform);
        var screenPos = args.Viewport.WorldToLocal(worldPos);
        screenPos.Y = args.Viewport.Size.Y - screenPos.Y;

        var playerAngle = playerXform.LocalRotation;
        var cardinalDirection = playerAngle.GetDir();
        var forwardVec = cardinalDirection.ToVec();

        var fovCosine = FieldOfViewSystem.GetFovCosine(visionComponent.Angle);

        var bounds = _spriteSystem.GetLocalBounds((playerEntity.Value, sprite));
        var worldRadius = bounds.Height;
        var pixelRadius = worldRadius * EyeManager.PixelsPerMeter * args.Viewport.RenderScale.Y;

        _shader.SetParameter("playerScreenPos", screenPos);
        _shader.SetParameter("playerForward", forwardVec);
        _shader.SetParameter("fovCosine", fovCosine);
        _shader.SetParameter("safeZoneRadius", pixelRadius);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
