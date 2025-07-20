using Content.Client._Scp.Shaders.Common;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Client._Scp.Shaders.FieldOfView;

public sealed class FieldOfViewOverlaySystem : ComponentOverlaySystem<FieldOfViewOverlay, FieldOfViewComponent>
{
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private EntityQuery<FieldOfViewComponent> _fovQuery;
    private EntityQuery<FOVHiddenSpriteComponent> _hiddenQuery;

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new FieldOfViewOverlay();

        _fovQuery = GetEntityQuery<FieldOfViewComponent>();
        _hiddenQuery = GetEntityQuery<FOVHiddenSpriteComponent>();

        Overlay.ConeOpacity = _configuration.GetCVar(ScpCCVars.FieldOfViewOpacity);
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewOpacity, OnOpacityChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;

        if (!_fovQuery.HasComp(player))
            return;

        var query = EntityQueryEnumerator<PhysicsComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var physics, out var sprite))
        {
            if (physics.BodyType == BodyType.Static)
                continue;

            if (player == uid)
                continue;

            var inFov = _fov.IsInViewAngle(player.Value, uid);

            if (sprite.Visible && !inFov && !_hiddenQuery.HasComp(uid))
            {
                HideSprite(uid, sprite);
                continue;
            }

            if (inFov && _hiddenQuery.HasComp(uid))
            {
                ShowSprite(uid, sprite);
            }
        }
    }

    protected override void OnPlayerDetached(Entity<FieldOfViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        base.OnPlayerDetached(ent, ref args);

        ShowAllHiddenSprites();
    }

    private void ShowAllHiddenSprites()
    {
        var query = EntityQueryEnumerator<FOVHiddenSpriteComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            ShowSprite(uid, sprite);
        }
    }

    private void HideSprite(EntityUid uid, SpriteComponent sprite)
    {
        _sprite.SetVisible((uid, sprite), false);
        AddComp<FOVHiddenSpriteComponent>(uid);
    }

    private void ShowSprite(EntityUid uid, SpriteComponent sprite)
    {
        _sprite.SetVisible((uid, sprite), true);
        RemComp<FOVHiddenSpriteComponent>(uid);
    }

    private void OnOpacityChanged(float option)
    {
        Overlay.ConeOpacity = option;
    }
}
