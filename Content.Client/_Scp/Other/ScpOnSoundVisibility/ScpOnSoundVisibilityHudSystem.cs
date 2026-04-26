using Content.Client.Overlays;
using Content.Client.SSDIndicator;
using Content.Shared._Scp.Other.ScpOnSoundVisibility;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Client._Scp.Other.ScpOnSoundVisibility;

public sealed partial class ScpOnSoundVisibilityHudSystem : EquipmentHudSystem<ScpOnSoundVisibilityViewerComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    internal readonly List<(Entity<SpriteComponent> Ent, float BaseAlpha)> CachedBaseAlphas = new(64);

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<MovementSpeedModifierComponent> _movementSpeedQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    private ScpOnSoundVisibilitySetAlphaOverlay _setAlphaOverlay = default!;
    private ScpOnSoundVisibilityResetAlphaOverlay _resetAlphaOverlay = default!;

    private ScpOnSoundVisibilityViewerComponent? _viewerComponent;

    private bool _overlaysPresented;
    private float _lastUpdateTime;
    private const float UpdateInterval = 0.05f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeOverlay();

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, GetStatusIconsEvent>(OnGetStatusIcons, after: [typeof(SSDIndicatorSystem)]);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, ExamineAttemptEvent>(OnExamine);

        SubscribeLocalEvent((Entity<ActiveScpOnSoundVisibilityComponent> ent, ref StartCollideEvent args)
            => OnCollide(ent, args.OtherEntity));
        SubscribeLocalEvent((Entity<ActiveScpOnSoundVisibilityComponent> ent, ref EndCollideEvent args)
            => OnCollide(ent, args.OtherEntity));
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, AfterAutoHandleStateEvent>(OnVisibilityStateUpdated);

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, DownedEvent>(OnDown);

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _movementSpeedQuery = GetEntityQuery<MovementSpeedModifierComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }

    public override void Shutdown()
    {
        RestoreCachedBaseAlphas();
        RemoveOverlays();

        _setAlphaOverlay.Dispose();
        _resetAlphaOverlay.Dispose();

        base.Shutdown();
    }

    private void OnVisibilityStateUpdated(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.LastHandledVisibilityResetCounter == ent.Comp.VisibilityResetCounter)
            return;

        ent.Comp.LastHandledVisibilityResetCounter = ent.Comp.VisibilityResetCounter;
        ent.Comp.VisibilityAcc = ScpOnSoundVisibilityComponent.InitialVisibilityAcc;
    }

    private void OnExamine(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!IsActive)
            return;

        var visibility = GetVisibility(ent);

        if (visibility < 0.2f)
            args.Cancel();
    }

    private void OnGetStatusIcons(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var visibility = GetVisibility(ent);

        if (visibility <= 0.5f)
            args.StatusIcons.Clear();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ScpOnSoundVisibilityViewerComponent> args)
    {
        base.UpdateInternal(args);

        _viewerComponent = args.Components.Count > 0 ? args.Components[0] : null;
        AddOverlays();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _viewerComponent = null;
        _lastUpdateTime = 0f;

        RestoreCachedBaseAlphas();
        RemoveOverlays();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
            return;

        _lastUpdateTime += frameTime;
        if (_lastUpdateTime < UpdateInterval)
            return;

        var delta = _lastUpdateTime;
        _lastUpdateTime = 0f;

        var query = EntityQueryEnumerator<ActiveScpOnSoundVisibilityComponent>();
        while (query.MoveNext(out _, out var visibilityComponent))
        {
            if (visibilityComponent.VisibilityAcc >= visibilityComponent.HideTime)
                continue;

            visibilityComponent.VisibilityAcc = MathF.Min(visibilityComponent.VisibilityAcc + delta, visibilityComponent.HideTime);
        }
    }

    internal bool CanDraw(in OverlayDrawArgs args)
    {
        if (!IsActive)
            return false;

        if (_playerManager.LocalEntity is not { } player)
            return false;

        if (!_eyeQuery.TryComp(player, out var eye))
            return false;

        return args.Viewport.Eye == eye.Eye;
    }

    internal void RestoreCachedBaseAlphas()
    {
        foreach (var (ent, baseAlpha) in CachedBaseAlphas)
        {
            if (!EntityManager.EntityExists(ent))
                continue;

            _sprite.SetColor(ent.AsNullable(), ent.Comp.Color.WithAlpha(baseAlpha));
        }

        CachedBaseAlphas.Clear();
    }

    internal static float GetVisibility(Entity<ActiveScpOnSoundVisibilityComponent> ent)
    {
        var acc = ent.Comp.VisibilityAcc;

        if (acc > ent.Comp.HideTime)
            return 0;

        return Math.Clamp(1f - (acc / ent.Comp.HideTime), 0f, 1f);
    }

    private void OnMove(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref MoveEvent args)
    {
        if (!IsActive)
            return;

        // В зависимости от наличие защит или проблем со зрением изменяется то, насколько хорошо мы видим жертву
        if (ModifyAcc(ent.Comp, out var modifier)) // Если зрение затруднено
        {
            ent.Comp.VisibilityAcc *= modifier;
        }
        else if (!TryComp<ScpOnSoundVisibilityViewerComponent>(ent, out var viewerComp) ||
            !_whitelist.IsWhitelistPass(viewerComp.Protections, ent)) // Если имеется защита(тихое хождение)
        {
            return;
        }
        else // Если со зрением все ок
        {
            ent.Comp.VisibilityAcc = 0;
        }

        if (!_movementSpeedQuery.TryComp(ent, out var speedModifierComponent)
            || !_physicsQuery.TryComp(ent, out var physicsComponent))
        {
            return;
        }

        var currentVelocity = physicsComponent.LinearVelocity.Length();

        if (speedModifierComponent.BaseWalkSpeed > currentVelocity)
            ent.Comp.VisibilityAcc = ent.Comp.HideTime / 2f;
    }


    private void OnCollide(Entity<ActiveScpOnSoundVisibilityComponent> ent, EntityUid otherEntity)
    {
        if (!IsActive)
            return;

        if (!HasComp<ScpOnSoundVisibilityViewerComponent>(otherEntity))
            return;

        MobDidSomething(ent);
    }

    private void OnThrow(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref ThrowEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void OnStood(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref StoodEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void OnMeleeAttack(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref MeleeAttackEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void OnDown(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref DownedEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void MobDidSomething(Entity<ActiveScpOnSoundVisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = ScpOnSoundVisibilityComponent.InitialVisibilityAcc;
    }

    // TODO: Переделать под статус эффект и добавить его в панель статус эффектов, а то непонятно игруну
    private bool ModifyAcc(ActiveScpOnSoundVisibilityComponent visibilityComponent, out int modifier)
    {
        // 1 = отсутствие модификатора
        modifier = 1;

        if (_viewerComponent == null)
            return false;

        if (!_viewerComponent.PoorEyesight)
            return false;

        modifier = _random.Next(visibilityComponent.MinValue, visibilityComponent.MaxValue);

        return true;
    }
}
