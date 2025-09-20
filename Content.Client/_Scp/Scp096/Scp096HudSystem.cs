using System.Diagnostics.CodeAnalysis;
using Content.Client._Scp.Scp096.Ui;
using Content.Client.Overlays;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Scp.Scp096;
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
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private Scp096UiWidget? _widget;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096TargetComponent, GetStatusIconsEvent>(OnGetStatusIcon);

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

        if (!TryGetPlayerEntity(out var scpEntity) || !scpEntity.Value.Comp.RageStartTime.HasValue)
        {
            _widget.Visible = false;
            return;
        }

        _widget.Visible = true;

        var elapsedTime = _gameTiming.CurTime - scpEntity.Value.Comp.RageStartTime;
        var remainingTime = scpEntity.Value.Comp.RageDuration - elapsedTime.Value;

        _widget.SetData(remainingTime.TotalSeconds, scpEntity.Value.Comp.Targets.Count);
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
        var playerEntity = _playerManager.LocalEntity;

        if (!Validate(playerEntity))
            return;

        if (ent.Comp.TargetedBy.Contains(playerEntity.Value)
            && _prototypeManager.TryIndex(ent.Comp.KillIconPrototype, out var killIconPrototype))
        {
            args.StatusIcons.Add(killIconPrototype);
        }
    }

    private bool Validate([NotNullWhen(true)] EntityUid? player)
    {
        return IsActive &&
               player.HasValue &&
               HasComp<Scp096Component>(player.Value);
    }

    private bool TryGetPlayerEntity([NotNullWhen(true)] out Entity<Scp096Component>? scpEntity)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        scpEntity = null;

        if (!TryComp<Scp096Component>(playerEntity, out var scp096Component))
            return false;

        scpEntity = (playerEntity.Value, scp096Component);

        return true;
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
