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
        SubscribeLocalEvent<FieldOfViewComponent, AfterAutoHandleStateEvent>(AfterHandleState);

        SubscribeLocalEvent<FieldOfViewComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void AfterHandleState(Entity<FieldOfViewComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity != ent)
            return;

        Overlay.NullifyComponents();
        Overlay.EntityOverride = ent.Comp.RelayEntity;
    }

    private void OnParentChanged(Entity<FieldOfViewComponent> ent, ref EntParentChangedMessage args)
    {
        if (_player.LocalEntity != ent)
            return;

        if (!_spriteQuery.TryComp(args.Transform.ParentUid, out var sprite))
            return;

        ShowSprite(args.Transform.ParentUid, ref sprite);
    }

    private void OnShutdown(Entity<FOVHiddenSpriteComponent> ent, ref ComponentShutdown args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Цикл обновления скрытия спрайтов за пределами FOV.
    /// В начале выбирает сущность, от лица которой будет скрытие. Это нужно для поддержки мехов и других сущностей, которым игрок "передает управление".
    /// Дальше проходится по трем большим группам - предметы, сущности и следы. И переключает их спрайт в зависимости от расположения сущности.
    /// Сам игрок и его Transform.ParentUid не скрываются.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextTimeUpdate)
            return;

        var player = _player.LocalEntity;

        if (!_fovQuery.TryComp(player, out var localFov))
            return;

        var chosenEntity = localFov.RelayEntity ?? player;

        if (!chosenEntity.HasValue)
            return;

        var playerParent = Transform(player.Value).ParentUid;
        var defaultAngle = localFov.Angle;
        var angleTolerance = localFov.AngleTolerance;

        var query = EntityQueryEnumerator<ItemComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(chosenEntity.Value, defaultAngle, angleTolerance,  uid, ref sprite);
        }

        var mobQuery = EntityQueryEnumerator<MobStateComponent, SpriteComponent>();

        while (mobQuery.MoveNext(out var uid, out _, out var sprite))
        {
            if (uid == player)
                continue;

            // Здесь остается именно парент игрока, так как в большинстве случаев
            // chosenEntity и будет этим парентом.
            if (uid == playerParent)
                continue;

            ManageSprites(chosenEntity.Value, defaultAngle, angleTolerance,  uid, ref sprite);
        }

        var footprintQuery = EntityQueryEnumerator<FootprintComponent, SpriteComponent>();

        while (footprintQuery.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(chosenEntity.Value, defaultAngle, angleTolerance,  uid, ref sprite);
        }

        _nextTimeUpdate = _timing.CurTime + _updateCooldown;
    }

    private void ManageSprites(EntityUid chosenEntity, Angle defaultAngle, Angle angleTolerance, EntityUid target, ref SpriteComponent sprite)
    {
        if (IsClientSide(target))
            return;

        var inFov = _fov.IsInViewAngle(chosenEntity, defaultAngle, angleTolerance, target);
        var isHidden = _hiddenQuery.HasComp(target);

        if (sprite.Visible && !inFov && !isHidden)
        {
            if (!_transform.InRange(chosenEntity, target, 25f))
                return;

            HideSprite(target, ref sprite);
            return;
        }

        if (inFov && isHidden)
        {
            ShowSprite(target, ref sprite);
        }
    }

    protected override void OnPlayerAttached(Entity<FieldOfViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        base.OnPlayerAttached(ent, ref args);

        Overlay.NullifyComponents();
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
