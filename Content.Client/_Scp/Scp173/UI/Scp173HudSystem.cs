using System.Diagnostics.CodeAnalysis;
using Content.Client.Overlays;
using Content.Shared._Scp.Scp173;
using Content.Shared.Inventory.Events;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Scp.Scp173.UI;

public sealed class Scp173HudSystem : EquipmentHudSystem<Scp173ReagentTrackerComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private Scp173UiWidget? _widget;

    protected override void UpdateInternal(RefreshEquipmentHudEvent<Scp173ReagentTrackerComponent> args)
    {
        base.UpdateInternal(args);
        EnsureWidgetExist();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        RemoveWidget();
    }

    private void EnsureWidgetExist()
    {
        if (_ui.ActiveScreen == null)
            return;

        var layoutContainer = _ui.ActiveScreen.FindControl<LayoutContainer>("ViewportContainer");

        _widget = new Scp173UiWidget();
        LayoutContainer.SetAnchorAndMarginPreset(_widget, LayoutContainer.LayoutPreset.TopRight, margin: 50);

        layoutContainer.AddChild(_widget);

        _widget.Visible = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
            return;

        if (_widget == null)
            return;

        if (!TryGetPlayerEntity(out var scpEntity) || !scpEntity.Value.Comp.IsInContainment)
        {
            _widget.Visible = false;
            return;
        }

        _widget.Visible = true;

        var current = scpEntity.Value.Comp.CurrentReagentAmount.Float();
        _widget.SetData(current, 500f);
    }

    private bool TryGetPlayerEntity([NotNullWhen(true)] out Entity<Scp173ReagentTrackerComponent>? scpEntity)
    {
        scpEntity = null;

        if (!TryComp<Scp173ReagentTrackerComponent>(_player.LocalEntity, out var component))
            return false;

        scpEntity = (_player.LocalEntity.Value, component);

        return true;
    }

    private void RemoveWidget()
    {
        if (_widget == null)
            return;

        _widget.Parent?.RemoveChild(_widget);
        _widget = null;
    }
}
