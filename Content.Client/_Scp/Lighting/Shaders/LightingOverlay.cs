using System.Numerics;
using Content.Shared._Scp.Lighting.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Scp.Lighting.Shaders;

public sealed class LightingOverlay : Overlay
{
    private readonly EntityManager _entityManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;

    private readonly EntityQuery<TransformComponent> _transformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> Shader = "LightingOverlay";

    private readonly Texture _maskTexture;
    private readonly Vector2 _maskOffset;

    public bool Enabled = true;

    public LightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        _entityManager = entityManager;
        _spriteSystem = entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _transformSystem = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();

        IoCManager.InjectDependencies(this);

        _shader = prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;

        _maskTexture = _spriteSystem.Frame0(LightingOverlayComponent.Mask);

        var xOffset = LightingOverlayComponent.MaskOffset.X - (_maskTexture.Width / 2f) / EyeManager.PixelsPerMeter;
        var yOffset = LightingOverlayComponent.MaskOffset.Y - (_maskTexture.Height / 2f) / EyeManager.PixelsPerMeter;
        _maskOffset = new Vector2(xOffset, yOffset);

        _transformQuery = _entityManager.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!Enabled)
            return;

        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);

        var query = _entityManager.AllEntityQueryEnumerator<LightingOverlayComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var pointLight, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            if (!pointLight.Enabled)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform, _transformQuery);

            if (!bounds.Contains(worldPos))
                continue;

            var (_, _, worldMatrix) = _transformSystem.GetWorldPositionRotationMatrix(xform, _transformQuery);
            handle.SetTransform(worldMatrix);

            handle.DrawTexture(_maskTexture, _maskOffset, pointLight.Color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
