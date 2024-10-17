using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client._Scp.Scp096.Ui;
using Content.Client.Overlays;
using Content.Shared._Scp.Scp096;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusIcon;
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

    private Scp096UiWidget? Widget => _uiManager.ActiveScreen?.GetWidget<Scp096UiWidget>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096TargetComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Widget == null)
        {
            return;
        }

        if (!IsActive)
        {
            Widget.Visible = false;
            return;
        }


        if (!TryGetPlayerEntity(out var scpEntity) || !scpEntity.Value.Comp.RageStartTime.HasValue)
        {
            Widget.Visible = false;
            return;
        }

        Widget.Visible = true;

        var elapsedTime = _gameTiming.CurTime - scpEntity.Value.Comp.RageStartTime;
        var remainingTime = scpEntity.Value.Comp.RageDuration - elapsedTime.Value.TotalSeconds;

        Widget.SetData(remainingTime, scpEntity.Value.Comp.Targets.Count);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        if (Widget != null)
        {
            Widget.Visible = false;
        }
    }

    private void OnGetStatusIcon(Entity<Scp096TargetComponent> ent, ref GetStatusIconsEvent args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (!Validate(playerEntity))
        {
            return;
        }

        if (!ent.Comp.TargetedBy.Contains(playerEntity.Value))
            return;

        var icon = _prototypeManager.Index<FactionIconPrototype>(ent.Comp.KillIconPrototype);
        args.StatusIcons.Add(icon);
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
        {
            return false;
        }

        scpEntity = new Entity<Scp096Component>(playerEntity.Value, scp096Component);

        return true;
    }

}
