using Content.Client._Scp.Shaders.FieldOfView.Overlays;
using Content.Client.Eye;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared.MouseRotator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.FieldOfView;

/// <summary>
///     Handles adding and removing the viewcone overlays, as well as ferrying data between them
///     Also handles calculating desired view angle for active viewcones so overlays can use it
/// </summary>
public sealed class FieldOfViewOverlayManagementSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private FieldOfViewConeOverlay _coneOverlay = default!;
    private FieldOfViewSetAlphaOverlay _setAlphaOverlay = default!;
    private FieldOfViewResetAlphaOverlay _resetAlphaOverlay = default!;

    private const float LerpHalfLife = 0.05f;

    // slightly balls state management, but
    // done so we don't have to requery within the same frame
    // this is always cleared at the end of resetting alpha
    // it is the least thread safe code of all time obviously. but rendering not threaded. so
    // we can abuse the fact that the overlays will always draw sequentially in the order we expect, and
    // one wont start rendering in the middle of rendering another
    [Access(typeof(FieldOfViewSetAlphaOverlay), typeof(FieldOfViewResetAlphaOverlay))]
    public List<(Entity<SpriteComponent> ent, float baseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldOfViewComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<FieldOfViewComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }


    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // the reason we use lerpingeye here in the query first is to
        // specifically check for eyes that we are actually rendering (lerpingeye already handles this sort of
        // its like jank as fuck in that system but whatever thats like not my problem )
        var enumerator = AllEntityQuery<LerpingEyeComponent, EyeComponent, FieldOfViewComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone, out var xform))
        {
            var eyeAngle = eye.Rotation;
            var rotation = _xform.GetWorldRotation(xform);
            var playerAngle = rotation;
            var desiredWasNull = viewcone.DesiredViewAngle == null;

            if (HasComp<MouseRotatorComponent>(uid))
            {
                var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
                if (mousePos.MapId != MapId.Nullspace)
                    playerAngle = (mousePos.Position - _xform.GetMapCoordinates(xform).Position).ToAngle() + Angle.FromDegrees(90);

                viewcone.LastMouseRotationAngle = playerAngle;
            }
            else if (viewcone.LastMouseRotationAngle != 0f)
            {
                // if last frame we had a mouse rotation angle, but now we dont,
                // that means it was disabled
                // but, we should keep the old mouse angle for viewcone, at least until the real angle actually changes
                if (MathHelper.CloseToPercent(viewcone.LastWorldRotationAngle, playerAngle, .000001d))
                {
                    playerAngle = viewcone.LastMouseRotationAngle;
                }
                else
                {
                    viewcone.LastMouseRotationAngle = 0f;
                }
            }

            viewcone.LastWorldRotationAngle = rotation;
            viewcone.DesiredViewAngle = playerAngle + eyeAngle;

            // if desired angle was null before we set it
            // then just set viewangle to it immediately
            // (assume it was first frame)
            if (desiredWasNull)
            {
                viewcone.ViewAngle = viewcone.DesiredViewAngle.Value;
                continue;
            }

            // framerate-independent lerp
            // https://twitter.com/FreyaHolmer/status/1757836988495847568
            // convert to angle first so we lerp thru shortestdistance
            viewcone.ViewAngle = Angle.Lerp(viewcone.ViewAngle, viewcone.DesiredViewAngle.Value, 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife)));
        }
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
        if (_playerManager.LocalEntity == ent)
            AddOverlays();
    }

    private void OnConeManShutdown(Entity<FieldOfViewComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == ent)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlayMan.AddOverlay(_coneOverlay);
        _overlayMan.AddOverlay(_setAlphaOverlay);
        _overlayMan.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlayMan.RemoveOverlay(_coneOverlay);
        _overlayMan.RemoveOverlay(_setAlphaOverlay);
        _overlayMan.RemoveOverlay(_resetAlphaOverlay);
    }
}
