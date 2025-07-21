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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<FieldOfViewComponent> _fovQuery;
    private EntityQuery<FOVHiddenSpriteComponent> _hiddenQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<FootprintComponent> _footprintQuery;

    private TimeSpan _nextTimeUpdate = TimeSpan.Zero;
    private TimeSpan _updateCooldown = TimeSpan.FromSeconds(0.1f);

    private const float UpdateRange = 20f;

    private bool _useAltMethod;

    public override void Initialize()
    {
        base.Initialize();

        Overlay = new FieldOfViewOverlay();

        _fovQuery = GetEntityQuery<FieldOfViewComponent>();
        _hiddenQuery = GetEntityQuery<FOVHiddenSpriteComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        _itemQuery = GetEntityQuery<ItemComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _footprintQuery = GetEntityQuery<FootprintComponent>();

        _useAltMethod = _configuration.GetCVar(ScpCCVars.FieldOfViewUseAltMethod);
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewUseAltMethod, b => _useAltMethod = b);

        Overlay.BlurScale = _configuration.GetCVar(ScpCCVars.FieldOfViewBlurScale);
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewBlurScale, OnBlurScaleChanged);

        _updateCooldown = TimeSpan.FromSeconds(_configuration.GetCVar(ScpCCVars.FieldOfViewCheckCooldown));
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewCheckCooldown, OnCheckCooldownChanged);

        Overlay.ConeOpacity = _configuration.GetCVar(ScpCCVars.FieldOfViewOpacity);
        _configuration.OnValueChanged(ScpCCVars.FieldOfViewOpacity, OnOpacityChanged);

        SubscribeLocalEvent<FOVHiddenSpriteComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FieldOfViewComponent, AfterAutoHandleStateEvent>(AfterHandleState);

        SubscribeLocalEvent<FieldOfViewComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeNetworkEvent<CameraSwitchedEvent>(OnSwitchMessage);
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

        ShowSprite(args.Transform.ParentUid);
    }

    private void OnShutdown(Entity<FOVHiddenSpriteComponent> ent, ref ComponentShutdown args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    // TODO: Лютый щиткод, который работает 50 на 50, но лучше, чем ничего
    private void OnSwitchMessage(CameraSwitchedEvent args)
    {
        var actor = GetEntity(args.Actor);

        if (_player.LocalEntity != actor)
            return;

        if (!_fovQuery.HasComp(actor))
            return;

        ShowAllHiddenSprites();
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

        _nextTimeUpdate = _timing.CurTime + _updateCooldown;

        var player = _player.LocalEntity;
        if (!_fovQuery.TryComp(player, out var localFov))
            return;

        var chosenEntity = localFov.RelayEntity ?? player;
        if (!chosenEntity.HasValue)
            return;

        var playerParent = _xformQuery.Comp(player.Value).ParentUid;
        var defaultAngle = localFov.Angle;
        var angleTolerance = localFov.AngleTolerance;

        UpdatePrimaryMethod(chosenEntity.Value, player.Value, playerParent, defaultAngle, angleTolerance);
        UpdateAltMethod(chosenEntity.Value, player.Value, playerParent, defaultAngle, angleTolerance);
    }

    private void UpdatePrimaryMethod(EntityUid chosenEntity, EntityUid player, EntityUid playerParent, Angle defaultAngle, Angle angleTolerance)
    {
        if (_useAltMethod)
            return;

        var query = EntityQueryEnumerator<ItemComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(chosenEntity, defaultAngle, angleTolerance,  uid, ref sprite);
        }

        var mobQuery = EntityQueryEnumerator<MobStateComponent, SpriteComponent>();

        while (mobQuery.MoveNext(out var uid, out _, out var sprite))
        {
            // Здесь остается именно парент игрока, так как в большинстве случаев
            // chosenEntity и будет этим парентом.
            if (uid == player || uid == playerParent)
                continue;

            ManageSprites(chosenEntity, defaultAngle, angleTolerance,  uid, ref sprite);
        }

        var footprintQuery = EntityQueryEnumerator<FootprintComponent, SpriteComponent>();

        while (footprintQuery.MoveNext(out var uid, out _, out var sprite))
        {
            ManageSprites(chosenEntity, defaultAngle, angleTolerance,  uid, ref sprite);
        }
    }


    private void UpdateAltMethod(EntityUid chosenEntity, EntityUid player, EntityUid playerParent, Angle defaultAngle, Angle angleTolerance)
    {
        if (!_useAltMethod)
            return;

        if (!_xformQuery.TryComp(chosenEntity, out var chosenXform))
            return;

        var entitiesInRange = _lookup.GetEntitiesInRange<SpriteComponent>(chosenXform.Coordinates, UpdateRange);

        foreach (var (uid, sprite) in entitiesInRange)
        {
            if (uid == player || uid == playerParent)
                continue;

            if (!CanBeHidden(uid))
                continue;

            if (_fov.IsInViewAngle(chosenEntity, defaultAngle, angleTolerance, uid))
                continue;

            HideSprite(uid, in sprite);
        }

        var toShowQuery = EntityQueryEnumerator<FOVHiddenSpriteComponent>();
        while (toShowQuery.MoveNext(out var uid, out _))
        {
            if (!_fov.IsInViewAngle(chosenEntity, defaultAngle, angleTolerance, uid))
                continue;

            ShowSprite(uid);
        }
    }

    private void ManageSprites(EntityUid chosenEntity, Angle defaultAngle, Angle angleTolerance, EntityUid target, ref SpriteComponent sprite)
    {
        if (IsClientSide(target))
            return;

        var inFov = _fov.IsInViewAngle(chosenEntity, defaultAngle, angleTolerance, target);
        var isHidden = _hiddenQuery.HasComp(target);

        if (sprite.Visible && !inFov && !isHidden)
        {
            // TODO: Щиткод затычка, которая нужна для камер
            if (!_transform.InRange(chosenEntity, target, 16f))
                return;

            HideSprite(target, in sprite);
            return;
        }

        if (inFov && isHidden)
        {
            ShowSprite(target);
        }
    }

    private bool CanBeHidden(EntityUid uid)
    {
        if (IsClientSide(uid))
            return false;

        if (_itemQuery.HasComp(uid))
            return true;

        if (_mobQuery.HasComp(uid))
            return true;

        if (_footprintQuery.HasComp(uid))
            return true;

        return false;
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
        var query = EntityQueryEnumerator<FOVHiddenSpriteComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            ShowSprite(uid);
        }
    }

    private void HideSprite(EntityUid uid, in SpriteComponent sprite)
    {
        if (!sprite.Visible)
            return;

        _sprite.SetVisible((uid, sprite), false);
        AddComp<FOVHiddenSpriteComponent>(uid);
    }

    private void ShowSprite(EntityUid uid)
    {
        RemComp<FOVHiddenSpriteComponent>(uid);
    }

    private void OnOpacityChanged(float option)
    {
        Overlay.ConeOpacity = option;
    }

    private void OnBlurScaleChanged(float option)
    {
        Overlay.BlurScale = option;
    }

    private void OnCheckCooldownChanged(float option)
    {
        _updateCooldown = TimeSpan.FromSeconds(option);
    }
}
