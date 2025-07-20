using Content.Client._Scp.Shaders.Common;
using Content.Shared._Scp.ScpCCVars;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Shaders.FieldOfView;

public sealed class FieldOfViewOverlaySystem : ComponentOverlaySystem<FieldOfViewOverlay, FieldOfViewComponent>
{
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<FieldOfViewComponent> _fovQuery;
    private EntityQuery<FOVHiddenSpriteComponent> _hiddenQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    private TimeSpan _nextTimeUpdate = TimeSpan.Zero;
    private readonly TimeSpan _updateCooldown = TimeSpan.FromSeconds(0.1f);

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new FieldOfViewOverlay();

        _fovQuery = GetEntityQuery<FieldOfViewComponent>();
        _hiddenQuery = GetEntityQuery<FOVHiddenSpriteComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        Overlay.ConeOpacity = _configuration.GetCVar(ScpCCVars.FieldOfViewOpacity);
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewOpacity, OnOpacityChanged);

        SubscribeLocalEvent<FOVHiddenSpriteComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<FOVHiddenSpriteComponent> ent, ref ComponentShutdown args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;

        if (!_fovQuery.HasComp(player))
            return;

        if (_timing.CurTime < _nextTimeUpdate)
            return;

        var query = EntityQueryEnumerator<ItemComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(player.Value, uid, ref sprite);
        }

        var mobQuery = EntityQueryEnumerator<MobStateComponent, SpriteComponent>();

        while (mobQuery.MoveNext(out var uid, out _, out var sprite))
        {
            if (uid == player)
                continue;

            ManageSprites(player.Value, uid, ref sprite);
        }

        var footprintQuery = EntityQueryEnumerator<FootprintComponent, SpriteComponent>();

        while (footprintQuery.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(player.Value, uid, ref sprite);
        }

        _nextTimeUpdate = _timing.CurTime + _updateCooldown;
    }

    private void ManageSprites(EntityUid player, EntityUid uid, ref SpriteComponent sprite)
    {
        if (IsClientSide(uid))
            return;

        var inFov = _fov.IsInViewAngle(player, uid);
        var isHidden = _hiddenQuery.HasComp(uid);

        if (sprite.Visible && !inFov && !isHidden)
        {
            if (!_transform.InRange(player, uid, 25f))
                return;

            HideSprite(uid, ref sprite);
            return;
        }

        if (inFov && isHidden)
        {
            ShowSprite(uid, ref sprite);
        }
    }

    protected override void OnPlayerAttached(Entity<FieldOfViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        base.OnPlayerAttached(ent, ref args);

        Overlay = new FieldOfViewOverlay();
        Overlay.ConeOpacity = _configuration.GetCVar(ScpCCVars.FieldOfViewOpacity);
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
            ShowSprite(uid, ref sprite);
        }
    }

    private void HideSprite(EntityUid uid, ref SpriteComponent sprite)
    {
        if (!sprite.Visible)
            return;

        _sprite.SetVisible((uid, sprite), false);
        AddComp<FOVHiddenSpriteComponent>(uid);
    }

    private void ShowSprite(EntityUid uid, ref SpriteComponent sprite)
    {
        if (sprite.Visible)
            return;

        RemComp<FOVHiddenSpriteComponent>(uid);
    }

    private void OnOpacityChanged(float option)
    {
        Overlay.ConeOpacity = option;
    }
}
