using System.Numerics;
using Content.Client._Scp.Shaders.FieldOfView.ComponentTree;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;

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

    private readonly EntityQuery<SpriteComponent> _spriteQuery;

    private readonly HashSet<EntityUid> _seen = [];

    private struct QueryState
    {
        public HashSet<EntityUid> Seen;
        public EntityQuery<SpriteComponent> SpriteQuery;
        public SpriteSystem SpriteSys;
        public TransformSystem Xform;
        public FieldOfViewOverlayManagementSystem FovManagement;

        public Vector2 EyePos;
        public Angle EyeRot;
        public float RadConeAngle;
        public float RadConeFeather;
        public float ConeIgnoreRadius;
        public float ConeIgnoreFeather;
        public EntityUid PlayerEntityUid;
    }

    private static bool QueryCallback(ref QueryState state, in ComponentTreeEntry<FieldOfViewOccludableComponent> entry)
    {
        var comp = entry.Component;
        var uid = entry.Uid;

        if (!state.Seen.Add(uid))
            return true;

        if (!state.SpriteQuery.TryComp(uid, out var sprite))
            return true;

        if (comp.Source == state.PlayerEntityUid)
            return true;

        if (!comp.OccludeIfAnchored && entry.Transform.Anchored)
            return true;

        var entPos = state.Xform.GetWorldPosition(entry.Transform);
        var dist = entPos - state.EyePos;
        var distLength = dist.Length();
        var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), state.EyeRot);

        var angleAlpha = (float)Math.Clamp((Math.Abs(angleDist.Theta) - (state.RadConeAngle * 0.5f)) + (state.RadConeFeather * 0.5f), 0f, state.RadConeFeather) / state.RadConeFeather;
        var distAlpha = Math.Clamp((distLength - state.ConeIgnoreRadius) + (state.ConeIgnoreFeather * 0.5f), 0f, state.ConeIgnoreFeather) / state.ConeIgnoreFeather;
        var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

        // микро-оптимизация - не трогать, если альфа почти не изменилась
        var newAlpha = comp.Inverted ? 1f - targetAlpha : targetAlpha;
        if (Math.Abs(sprite.Color.A - newAlpha) <= 0.001f)
            return true;

        var ent = (uid, sprite);

        // сохраняем старую альфу для восстановления
        state.FovManagement.CachedBaseAlphas.Add((ent, sprite.Color.A));

        // применяем новую цвет/альфу
        state.SpriteSys.SetColor(ent, sprite.Color.WithAlpha(newAlpha));

        return true;
    }

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
        var eyeRot = cone.CurrentAngle - eye.Rotation;

        var radConeAngle = MathHelper.DegreesToRadians(cone.Angle);
        var radConeFeather = MathHelper.DegreesToRadians(cone.AngleTolerance);

        _fovManagement.CachedBaseAlphas.Clear();
        _seen.Clear();

        var worldBounds = args.WorldBounds;
        foreach (var (treeUid, treeComp) in _tree.GetIntersectingTrees(args.MapId, worldBounds))
        {
            var bounds = _xform.GetInvWorldMatrix(treeUid).TransformBox(worldBounds);

            // Чтобы избежать больших нагрузок на GC, пришлось изобретать велосипед, так как деревья не поддерживают передачу готового списка.
            // Создаем вручную стейты и обрабатываем их, используя заранее заготовленный HashSet().
            // Это выигрывает ~3 FPS на dev карте. В реальной игре при большом количестве скрывающихся элементов это будет ценнее.

            var state = new QueryState
            {
                Seen = _seen,
                SpriteQuery = _spriteQuery,
                SpriteSys = _sprite,
                Xform = _xform,
                FovManagement = _fovManagement,

                EyePos = eyePos,
                EyeRot = eyeRot,
                RadConeAngle = radConeAngle,
                RadConeFeather = radConeFeather,
                ConeIgnoreRadius = cone.ConeIgnoreRadius,
                ConeIgnoreFeather = cone.ConeIgnoreFeather,
                PlayerEntityUid = ent,
            };

            treeComp.Tree.QueryAabb(ref state, QueryCallback, bounds, true);
        }
    }
}
