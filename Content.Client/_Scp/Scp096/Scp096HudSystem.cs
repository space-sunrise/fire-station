using System.Diagnostics.CodeAnalysis;
using Content.Client.Overlays;
using Content.Shared._Scp.Scp096;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Scp096;

public sealed class Scp096HudSystem : EquipmentHudSystem<Scp096Component>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096TargetComponent, GetStatusIconsEvent>(OnStatusIcon);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<Scp096Component> args)
    {
        base.UpdateInternal(args);

        var player = _playerManager.LocalSession?.AttachedEntity;

        if (_overlayManager.HasOverlay<Scp096Overlay>()
            || !TryComp<Scp096Component>(player, out var scp096Component))
        {
            return;
        }

        var scpEntity = new Entity<Scp096Component>(player.Value, scp096Component);
        var scpOverlay = new Scp096Overlay(_entityLookup, scpEntity, _eyeManager);

        _overlayManager.AddOverlay(scpOverlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayManager.RemoveOverlay<Scp096Overlay>();
    }

    private void OnStatusIcon(Entity<Scp096TargetComponent> ent, ref GetStatusIconsEvent args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (!Validate(playerEntity))
        {
            return;
        }

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
}
