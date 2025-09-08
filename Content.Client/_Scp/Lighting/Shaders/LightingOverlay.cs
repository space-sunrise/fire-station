using System.Numerics;
using Content.Shared._Scp.Lighting.Shaders;
using Content.Shared._Scp.ScpCCVars;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Scp.Lighting.Shaders;

public sealed class LightingOverlay : Overlay
{
    private readonly EntityManager _entityManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;
    private readonly IConfigurationManager _cfg;
    private readonly IPrototypeManager _prototypeManager;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> Shader = "LightingOverlay";
    private bool _enableGlowing;

    public LightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        _entityManager = entityManager;
        _spriteSystem = entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _prototypeManager = prototypeManager;
        _transformSystem = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();
        _cfg = IoCManager.Resolve<IConfigurationManager>();
        _cfg.OnValueChanged(ScpCCVars.EnableLightsGlowing, val => _enableGlowing = val, true);

        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_enableGlowing)
            return;

        if (ScreenTexture == null)
            return;

        var xformCompQuery = _entityManager.GetEntityQuery<TransformComponent>();
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);

        var query = _entityManager.AllEntityQueryEnumerator<LightingOverlayComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out _, out var component, out var pointLight, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            if (!pointLight.Enabled)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform, xformCompQuery);

            if (!bounds.Contains(worldPos))
                continue;

            var color = component.Color ?? pointLight.Color;
            var (_, _, worldMatrix) = _transformSystem.GetWorldPositionRotationMatrix(xform, xformCompQuery);
            handle.SetTransform(worldMatrix);

            var mask = _spriteSystem.Frame0(component.Sprite); // mask
            var xOffset = component.Offsetx - (mask.Width / 2) / EyeManager.PixelsPerMeter;
            var yOffset = component.Offsety - (mask.Height / 2) / EyeManager.PixelsPerMeter;
            var textureVector = new Vector2(xOffset, yOffset);

            handle.DrawTexture(mask, textureVector, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
