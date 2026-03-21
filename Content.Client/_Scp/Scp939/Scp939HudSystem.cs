using Content.Client.Overlays;
using Content.Client.SSDIndicator;
using Content.Client.Stealth;
using Content.Shared._Scp.Scp939;
using Content.Shared._Scp.Scp939.Protection;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client._Scp.Scp939;

public sealed class Scp939HudSystem : EquipmentHudSystem<Scp939Component>
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    internal readonly List<(Entity<SpriteComponent> Ent, float BaseAlpha)> CachedBaseAlphas = new(64);

    private Scp939SetAlphaOverlay _setAlphaOverlay = default!;
    private Scp939ResetAlphaOverlay _resetAlphaOverlay = default!;

    // TODO: Выделить значения плохого зрения в отдельный компонент, не связанный с 939
    private Scp939Component? _scp939Component;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<Scp939ProtectionComponent> _scp939ProtectionQuery;
    private EntityQuery<MovementSpeedModifierComponent> _movementSpeedQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    private bool _overlaysPresented;
    private float _lastUpdateTime;

    private const float UpdateInterval = 0.05f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent((Entity<ActiveScp939VisibilityComponent> ent, ref StartCollideEvent args)
            => OnCollide(ent, args.OtherEntity));
        SubscribeLocalEvent((Entity<ActiveScp939VisibilityComponent> ent, ref EndCollideEvent args)
            => OnCollide(ent, args.OtherEntity));

        #region Visibility

        SubscribeLocalEvent<ActiveScp939VisibilityComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<ActiveScp939VisibilityComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<ActiveScp939VisibilityComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<ActiveScp939VisibilityComponent, MeleeAttackEvent>(OnMeleeAttack);

        #endregion

        SubscribeLocalEvent<ActiveScp939VisibilityComponent, GetStatusIconsEvent>(OnGetStatusIcons, after: [typeof(SSDIndicatorSystem)] );
        SubscribeLocalEvent<ActiveScp939VisibilityComponent, ExamineAttemptEvent>(OnExamine);

        SubscribeLocalEvent<Scp939Component, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp939Component, PlayerDetachedEvent>(OnPlayerDetached);

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _scp939ProtectionQuery = GetEntityQuery<Scp939ProtectionComponent>();
        _movementSpeedQuery = GetEntityQuery<MovementSpeedModifierComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();

        UpdatesAfter.Add(typeof(StealthSystem));
    }

    public override void Shutdown()
    {
        RestoreCachedBaseAlphas();
        RemoveOverlays();

        _setAlphaOverlay.Dispose();
        _resetAlphaOverlay.Dispose();

        base.Shutdown();
    }

    private void OnExamine(Entity<ActiveScp939VisibilityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!IsActive)
            return;

        var visibility = GetVisibility(ent);

        if (visibility < 0.2f)
            args.Cancel();
    }

    private void OnGetStatusIcons(Entity<ActiveScp939VisibilityComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var visibility = GetVisibility(ent);

        if (visibility <= 0.5f)
            args.StatusIcons.Clear();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<Scp939Component> args)
    {
        base.UpdateInternal(args);

        _scp939Component = args.Components.Count > 0 ? args.Components[0] : null;
        AddOverlays();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _scp939Component = null;
        _lastUpdateTime = 0f;

        RestoreCachedBaseAlphas();
        RemoveOverlays();
    }

    #region Visibility

    private void OnCollide(Entity<ActiveScp939VisibilityComponent> ent, EntityUid otherEntity)
    {
        if (!IsActive)
            return;

        if (!HasComp<Scp939Component>(otherEntity))
            return;

        MobDidSomething(ent);
    }

    private void OnThrow(Entity<ActiveScp939VisibilityComponent> ent, ref ThrowEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void OnStood(Entity<ActiveScp939VisibilityComponent> ent, ref StoodEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void OnMeleeAttack(Entity<ActiveScp939VisibilityComponent> ent, ref MeleeAttackEvent args)
    {
        if (!IsActive)
            return;

        MobDidSomething(ent);
    }

    private void MobDidSomething(Entity<ActiveScp939VisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = Scp939VisibilityComponent.InitialVisibilityAcc;
    }

    private void OnMove(Entity<ActiveScp939VisibilityComponent> ent, ref MoveEvent args)
    {
        if (!IsActive)
            return;

        // В зависимости от наличие защит или проблем со зрением у 939 изменяется то, насколько хорошо мы видим жертву
        if (ModifyAcc(ent.Comp, out var modifier)) // Если зрение затруднено
        {
            ent.Comp.VisibilityAcc *= modifier;
        }
        else if (_scp939ProtectionQuery.HasComp(ent)) // Если имеется защита(тихое хождение)
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

    #endregion

    private void OnPlayerAttached(Entity<Scp939Component> ent, ref PlayerAttachedEvent args)
    {
        _scp939Component = ent.Comp;
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<Scp939Component> ent, ref PlayerDetachedEvent args)
    {
        _scp939Component = null;
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

        var query = EntityQueryEnumerator<ActiveScp939VisibilityComponent>();
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

    internal static float GetVisibility(Entity<ActiveScp939VisibilityComponent> ent)
    {
        var acc = ent.Comp.VisibilityAcc;

        if (acc > ent.Comp.HideTime)
            return 0;

        return Math.Clamp(1f - (acc / ent.Comp.HideTime), 0f, 1f);
    }

    private void AddOverlays()
    {
        if (_overlaysPresented)
            return;

        _overlayManager.AddOverlay(_setAlphaOverlay);
        _overlayManager.AddOverlay(_resetAlphaOverlay);

        _overlaysPresented = true;
    }

    private void RemoveOverlays()
    {
        if (!_overlaysPresented)
            return;

        _overlayManager.RemoveOverlay(_setAlphaOverlay);
        _overlayManager.RemoveOverlay(_resetAlphaOverlay);

        CachedBaseAlphas.Clear();
        _overlaysPresented = false;
    }

    // TODO: Переделать под статус эффект и добавить его в панель статус эффектов, а то непонятно игруну
    /// <summary>
    /// Если вдруг собачка плохо видит
    /// </summary>
    private bool ModifyAcc(ActiveScp939VisibilityComponent visibilityComponent, out int modifier)
    {
        // 1 = отсутствие модификатора
        modifier = 1;

        if (_scp939Component == null)
            return false;

        if (!_scp939Component.PoorEyesight)
            return false;

        modifier = _random.Next(visibilityComponent.MinValue, visibilityComponent.MaxValue);

        return true;
    }
}
