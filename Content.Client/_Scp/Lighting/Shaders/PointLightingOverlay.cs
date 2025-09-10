using System.Numerics;
using Content.Shared._Scp.Lighting.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Scp.Lighting.Shaders;

public sealed class PointLightingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> Shader = "LightingOverlayStrong";

    private readonly Texture _pointTexture;
    private readonly Vector2 _pointOffset;

    public List<(TransformComponent xform, Matrix3x2 matrix, Vector2 worldPos, Color color)> Entities = [];
    public bool Enabled = true;

    public PointLightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        var spriteSystem = entityManager.System<SpriteSystem>();

        _shader = prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;

        _pointTexture = spriteSystem.Frame0(LightingOverlayComponent.Point);

        var xOffset = LightingOverlayComponent.PointOffset.X - (_pointTexture.Width / 2f) / EyeManager.PixelsPerMeter;
        var yOffset = LightingOverlayComponent.PointOffset.Y - (_pointTexture.Height / 2f) / EyeManager.PixelsPerMeter;
        _pointOffset = new Vector2(xOffset, yOffset);
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

        foreach (var (xform, matrix, worldPos, color) in Entities)
        {
            if (xform.MapID != args.MapId)
                continue;

            if (!bounds.Contains(worldPos))
                continue;

            handle.SetTransform(matrix);
            handle.DrawTexture(_pointTexture, _pointOffset, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
