using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Scp.Shaders.Bloom;

public sealed class MaskLightingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> Shader = "LightingOverlay";

    private readonly Texture _maskTexture;
    private readonly Vector2 _maskOffset;

    public List<(TransformComponent xform, Matrix3x2 matrix, Vector2 worldPos, Color color)> Entities = [];
    public bool Enabled = true;

    public MaskLightingOverlay(IPrototypeManager prototypeManager, SpriteSystem spriteSystem)
    {
        IoCManager.InjectDependencies(this);

        _shader = prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;

        _maskTexture = spriteSystem.Frame0(BloomOverlayVisualsComponent.Mask);

        var xOffset = BloomOverlayVisualsComponent.MaskOffset.X - (_maskTexture.Width / 2f) / EyeManager.PixelsPerMeter;
        var yOffset = BloomOverlayVisualsComponent.MaskOffset.Y - (_maskTexture.Height / 2f) / EyeManager.PixelsPerMeter;
        _maskOffset = new Vector2(xOffset, yOffset);
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
            handle.DrawTexture(_maskTexture, _maskOffset, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
