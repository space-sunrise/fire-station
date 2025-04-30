﻿using Content.Client.Overlays;
using Content.Shared._Scp.Blinking;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Blinking;

public sealed class ShowBlinkIconSystem : EquipmentHudSystem<ShowBlinkableComponent>
{
    [Dependency] private readonly BlinkingSystem _blinking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<BlinkIconPrototype> ClosedEyeIcon = "ClosedEyeIcon";
    private static BlinkIconPrototype _icon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, GetStatusIconsEvent>(OnGetIcon);

        _icon = _prototype.Index(ClosedEyeIcon);
    }

    private void OnGetIcon(Entity<BlinkableComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        if (!_blinking.AreEyesClosed(ent.AsNullable()))
            return;

        args.StatusIcons.Add(_icon);
    }
}
