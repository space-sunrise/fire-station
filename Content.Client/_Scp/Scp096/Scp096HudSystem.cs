using System.Diagnostics.CodeAnalysis;
using Content.Client._Scp.Scp096.Ui;
using Content.Client.Overlays;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096HudSystem : EquipmentHudSystem<Scp096Component>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly Scp096System _scp096 = default!;

    private EntityQuery<Scp096Component> _scp096Query;
    private EntityQuery<ActiveScp096RageComponent> _rageQuery;

    private Scp096UiWidget? _widget;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096TargetComponent, GetStatusIconsEvent>(OnGetStatusIcon);

        _scp096Query = GetEntityQuery<Scp096Component>();
        _rageQuery = GetEntityQuery<ActiveScp096RageComponent>();

        var gameplayStateLoad = _uiManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += EnsureWidgetExist;
        gameplayStateLoad.OnScreenUnload += RemoveWidget;
    }

    private void EnsureWidgetExist()
    {
        if (_widget != null)
            return;

        if (_uiManager.ActiveScreen == null)
            return;

        var layoutContainer = _uiManager.ActiveScreen.FindControl<LayoutContainer>("ViewportContainer");

        _widget = new Scp096UiWidget();
        LayoutContainer.SetAnchorAndMarginPreset(_widget, LayoutContainer.LayoutPreset.CenterTop, margin: 50);

        layoutContainer.AddChild(_widget);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
            return;

        if (_widget == null)
            return;

        if (!_rageQuery.TryComp(_player.LocalEntity, out var rage) || !rage.RageStartTime.HasValue)
        {
            _widget.Visible = false;
            return;
        }

        if (!_scp096Query.TryComp(_player.LocalEntity, out var scp096))
        {
            _widget.Visible = false;
            return;
        }

        _widget.Visible = true;

        var elapsedTime = _gameTiming.CurTime - rage.RageStartTime;
        var remainingTime = scp096.RageDuration - elapsedTime.Value;

        // TODO: Все таки записывать внутрь скромника хотя бы количество его таргетов, чтобы не делать это.
        _widget.SetData(remainingTime.TotalSeconds, _scp096.GetTargets().Count);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        RemoveWidget();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<Scp096Component> args)
    {
        base.UpdateInternal(args);

        EnsureWidgetExist();
    }

    private void OnGetStatusIcon(Entity<Scp096TargetComponent> ent, ref GetStatusIconsEvent args)
    {
        var playerEntity = _player.LocalEntity;

        if (!Validate(playerEntity))
            return;

        if (!_prototypeManager.TryIndex(ent.Comp.KillIconPrototype, out var killIconPrototype))
            return;

        args.StatusIcons.Add(killIconPrototype);
    }

    private bool Validate([NotNullWhen(true)] EntityUid? player)
    {
        return IsActive && _scp096Query.HasComp(player);
    }

    private void RemoveWidget()
    {
        if (_widget == null)
            return;

        _widget.Parent?.RemoveChild(_widget);
        _widget.RemoveAllChildren();
        _widget = null;
    }
}
