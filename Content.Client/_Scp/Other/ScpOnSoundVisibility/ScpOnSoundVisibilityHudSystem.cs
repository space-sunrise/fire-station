using System.Diagnostics.CodeAnalysis;
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
using Robust.Shared.Timing;

namespace Content.Client._Scp.Other.ScpOnSoundVisibility;

public sealed partial class ScpOnSoundVisibilityHudSystem : EquipmentHudSystem<ScpOnSoundVisibilityViewerComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public readonly List<(Entity<SpriteComponent> Ent, float BaseAlpha)> CachedBaseAlphas = new(64);

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<MovementSpeedModifierComponent> _movementSpeedQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ScpOnSoundVisibilityViewerComponent> _viewerQuery;

    private ScpOnSoundVisibilitySetAlphaOverlay _setAlphaOverlay = default!;
    private ScpOnSoundVisibilityResetAlphaOverlay _resetAlphaOverlay = default!;

    private bool _overlaysPresented;
    private TimeSpan _nextUpdateTime;
    private readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);

    public override void Initialize()
    {
        base.Initialize();

        InitializeOverlay();

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, GetStatusIconsEvent>(OnGetStatusIcons, after: [typeof(SSDIndicatorSystem)]);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, ExamineAttemptEvent>(OnExamine);

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<ActiveScpOnSoundVisibilityComponent, DownedEvent>(OnDown);

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _movementSpeedQuery = GetEntityQuery<MovementSpeedModifierComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _viewerQuery = GetEntityQuery<ScpOnSoundVisibilityViewerComponent>();

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

    private void OnExamine(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!IsActive)
            return;

        if (!TryGetLocalViewer(out var viewer))
            return;

        var visibility = GetVisibility(ent);

        if (visibility < viewer.Value.Comp.ExamineHideThreshold)
            args.Cancel();
    }

    private void OnGetStatusIcons(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        if (!TryGetLocalViewer(out var viewer))
            return;

        var visibility = GetVisibility(ent);

        if (visibility <= viewer.Value.Comp.StatusIconClearThreshold)
            args.StatusIcons.Clear();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ScpOnSoundVisibilityViewerComponent> args)
    {
        base.UpdateInternal(args);

        AddOverlays();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _nextUpdateTime = TimeSpan.Zero;

        RestoreCachedBaseAlphas();
        RemoveOverlays();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
            return;

        if (_timing.RealTime < _nextUpdateTime)
            return;

        if (!TryGetLocalViewer(out var viewer))
            return;

        var delta = (float)(_timing.RealTime - (_nextUpdateTime - UpdateInterval)).TotalSeconds;
        _nextUpdateTime = _timing.RealTime + UpdateInterval;

        var query = EntityQueryEnumerator<ActiveScpOnSoundVisibilityComponent>();
        while (query.MoveNext(out var visibilityEnt, out var visibilityComponent))
        {
            if (visibilityComponent.OnCollide)
                continue;

            if (viewer.Value.Owner == visibilityEnt)
                continue;

            if (visibilityComponent.VisibilityAcc >= visibilityComponent.HideTime)
                continue;

            visibilityComponent.VisibilityAcc = MathF.Min(visibilityComponent.VisibilityAcc + delta, visibilityComponent.HideTime);
        }
    }

    public bool CanDraw(in OverlayDrawArgs args)
    {
        if (!IsActive)
            return false;

        if (_playerManager.LocalEntity is not { } player)
            return false;

        if (!_eyeQuery.TryComp(player, out var eye))
            return false;

        return args.Viewport.Eye == eye.Eye;
    }

    public void RestoreCachedBaseAlphas()
    {
        foreach (var (ent, baseAlpha) in CachedBaseAlphas)
        {
            if (!EntityManager.EntityExists(ent))
                continue;

            _sprite.SetColor(ent.AsNullable(), ent.Comp.Color.WithAlpha(baseAlpha));
        }

        CachedBaseAlphas.Clear();
    }

    public static float GetVisibility(Entity<ActiveScpOnSoundVisibilityComponent> ent)
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

        if (!TryGetLocalViewer(out var viewer))
            return;

        if (ent.Comp.OnCollide)
            return;

        if (ent.Owner == viewer.Value.Owner)
            return;

        // В зависимости от наличие защит или проблем со зрением изменяется то, насколько хорошо мы видим жертву
        if (ModifyAcc(ent, out var modifier)) // Если зрение затруднено
        {
            ent.Comp.VisibilityAcc *= modifier;
        }
        else if (_whitelist.IsWhitelistPass(viewer.Value.Comp.Protections, ent)) // Если имеется защита(тихое хождение)
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

    private void OnStartCollide(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref StartCollideEvent args)
    {
        if (!IsActive)
            return;

        ent.Comp.OnCollide = true;
        MobDidSomething(ent);
    }

    private void OnEndCollide(Entity<ActiveScpOnSoundVisibilityComponent> ent, ref EndCollideEvent args)
    {
        if (!IsActive)
            return;

        ent.Comp.OnCollide = false;
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
    private bool ModifyAcc(Entity<ActiveScpOnSoundVisibilityComponent> ent, out float modifier)
    {
        // 1 = отсутствие модификатора
        modifier = 1f;

        if (!TryGetLocalViewer(out var viewer))
            return false;

        if (!viewer.Value.Comp.PoorEyesight)
            return false;

        modifier = _random.NextFloat(ent.Comp.MinValue, ent.Comp.MaxValue);

        return true;
    }

    private bool TryGetLocalViewer([NotNullWhen(true)] out Entity<ScpOnSoundVisibilityViewerComponent>? ent)
    {
        ent = null;
        var localPlayer = _playerManager.LocalEntity;

        if (localPlayer == null)
            return false;

        if (!_viewerQuery.TryComp(localPlayer, out var comp))
            return false;

        ent = (localPlayer.Value, comp);
        return true;
    }
}
