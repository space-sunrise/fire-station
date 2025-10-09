using Content.Client._Scp.Shaders.FieldOfView.ComponentTree;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Scp.Shaders.FieldOfView.Overlays;

/// <summary>
///     Queries the bounds for each viewport for all <see cref="FieldOfViewOccludableComponent"/>, then
///     sets their alpha before entities render in accordance with whether they should be in view or not
///
///     This alpha pass only works because of <see cref="FieldOfViewResetAlphaOverlay"/>, which resets in a later stage of rendering.
/// </summary>
public sealed class FieldOfViewSetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly FieldOfViewOverlayManagementSystem _fovManagement;
    private readonly FieldOfViewOccludableTreeSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public FieldOfViewSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _fovManagement = _ent.System<FieldOfViewOverlayManagementSystem>();
        _tree = _ent.System<FieldOfViewOccludableTreeSystem>();
        _xform  = _ent.System<TransformSystem>();
        _sprite = _ent.System<SpriteSystem>();

        _spriteQuery = _ent.GetEntityQuery<SpriteComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        if (!_fovManagement.PlayerEntity.HasValue)
            return false;

        var player = _fovManagement.PlayerEntity;

        if (args.Viewport.Eye != player.Value.Comp1.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_fovManagement.PlayerEntity.HasValue)
            return;

        var (ent, eye, cone, eyeTransform) = _fovManagement.PlayerEntity.Value;

        var eyePos = _xform.GetWorldPosition(eyeTransform);
        var eyeRot = cone.ViewAngle - eye.Rotation; // subtract rotation cuz idk. the lerp adds it but this doesnt want it for some reason idk.

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !! Thank You Bhijn God (TYBG) for 95% of the rest of this methods code !!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        var radConeAngle = MathHelper.DegreesToRadians(cone.Angle);
        var radConeFeather = MathHelper.DegreesToRadians(cone.AngleTolerance);

        _fovManagement.CachedBaseAlphas.Clear();
        var occludables = _tree.QueryAabb(args.MapId, args.WorldBounds);
        foreach (var entry in occludables)
        {
            var (comp, xform) = entry;
            var uid = entry.Uid; // this uses component.Owner.. oh well

            if (!_spriteQuery.TryComp(uid, out var sprite))
                continue;

            if (comp.Source == ent)
                continue;

            if (!comp.OccludeIfAnchored && xform.Anchored)
                continue;

            var entPos = _xform.GetWorldPosition(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

            var angleAlpha = (float) Math.Clamp((Math.Abs(angleDist.Theta) - (radConeAngle * 0.5f)) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp((distLength - cone.ConeIgnoreRadius) + (cone.ConeIgnoreFeather * 0.5f), 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

            // save the results so we can use it in resetalpha overlay
            _fovManagement.CachedBaseAlphas.Add(((uid, sprite), sprite.Color.A));

            var alpha = comp.Inverted ? 1f - targetAlpha : targetAlpha;
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
        }
    }
}
