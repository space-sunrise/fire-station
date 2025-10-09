using Content.Client._Scp.Shaders.FieldOfView.Overlays;
using Content.Client.Eye;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.FieldOfView;

/// <summary>
///     Handles adding and removing the viewcone overlays, as well as ferrying data between them
///     Also handles calculating desired view angle for active viewcones so overlays can use it
/// </summary>
public sealed class FieldOfViewOverlayManagementSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private FieldOfViewConeOverlay _coneOverlay = default!;
    private FieldOfViewSetAlphaOverlay _setAlphaOverlay = default!;
    private FieldOfViewResetAlphaOverlay _resetAlphaOverlay = default!;

    private const float LerpHalfLife = 0.05f;

    private EntityQuery<FieldOfViewComponent> _fovQuery;
    private EntityQuery<LerpingEyeComponent> _lerpingEyeQuery;
    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public Entity<EyeComponent, FieldOfViewComponent, TransformComponent>? PlayerEntity;

    // slightly balls state management, but
    // done so we don't have to requery within the same frame
    // this is always cleared at the end of resetting alpha
    // it is the least thread safe code of all time obviously. but rendering not threaded. so
    // we can abuse the fact that the overlays will always draw sequentially in the order we expect, and
    // one wont start rendering in the middle of rendering another
    [Access(typeof(FieldOfViewSetAlphaOverlay), typeof(FieldOfViewResetAlphaOverlay))]
    public readonly List<(Entity<SpriteComponent> ent, float baseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldOfViewComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<FieldOfViewComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _fovQuery = GetEntityQuery<FieldOfViewComponent>();
        _lerpingEyeQuery = GetEntityQuery<LerpingEyeComponent>();
        _eyeQuery = GetEntityQuery<EyeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!PlayerEntity.HasValue || !Exists(_player.LocalEntity))
        {
            ValidateEntity();
            return;
        }

        if (_player.LocalEntity != PlayerEntity.Value)
        {
            ValidateEntity();
            return;
        }

        if (PlayerEntity.Value.Comp1.Deleted)
        {
            ValidateEntity();
            return;
        }

        if (PlayerEntity.Value.Comp2.Deleted)
        {
            ValidateEntity();
            return;
        }

        if (PlayerEntity.Value.Comp3.Deleted)
        {
            ValidateEntity();
            return;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!PlayerEntity.HasValue)
            return;

        var player = PlayerEntity.Value;

        var eyeAngle = player.Comp1.Rotation;
        var rotation = _xform.GetWorldRotation(player.Comp3);
        var desiredWasNull = player.Comp2.DesiredViewAngle == null;

        player.Comp2.DesiredViewAngle = rotation + eyeAngle;

        // if desired angle was null before we set it
        // then just set viewangle to it immediately
        // (assume it was first frame)
        if (desiredWasNull)
        {
            player.Comp2.ViewAngle = player.Comp2.DesiredViewAngle.Value;
            return;
        }

        // framerate-independent lerp
        // https://twitter.com/FreyaHolmer/status/1757836988495847568
        // convert to angle first so we lerp thru shortestdistance
        player.Comp2.ViewAngle = Angle.Lerp(player.Comp2.ViewAngle, player.Comp2.DesiredViewAngle.Value, 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife)));
    }

    private void ValidateEntity()
    {
        PlayerEntity = null;

        if (!_player.LocalEntity.HasValue)
            return;

        var player = _player.LocalEntity.Value;

        if (!_fovQuery.TryComp(player, out var fov))
            return;

        if (!_lerpingEyeQuery.HasComp(player))
            return;

        if (!_eyeQuery.TryComp(player, out var eye))
            return;

        if (!_xformQuery.TryComp(player, out var xform))
            return;

        PlayerEntity = (player, eye, fov, xform);
    }

    private void OnPlayerAttached(Entity<FieldOfViewComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<FieldOfViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void OnConeManInit(Entity<FieldOfViewComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent)
            AddOverlays();
    }

    private void OnConeManShutdown(Entity<FieldOfViewComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlay.AddOverlay(_coneOverlay);
        _overlay.AddOverlay(_setAlphaOverlay);
        _overlay.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlay.RemoveOverlay(_coneOverlay);
        _overlay.RemoveOverlay(_setAlphaOverlay);
        _overlay.RemoveOverlay(_resetAlphaOverlay);
    }
}
