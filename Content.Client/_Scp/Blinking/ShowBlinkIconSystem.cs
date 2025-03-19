﻿using Content.Client.Overlays;
using Content.Shared._Scp.Blinking;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Blinking;

public sealed class ShowBlinkIconSystem : EquipmentHudSystem<ShowBlinkableComponent>
{
    [Dependency] private readonly BlinkingSystem _blinkingSystem = default!;
    [Dependency] private readonly SharedEyeClosingSystem _eyeClosing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlinkableComponent, GetStatusIconsEvent>(OnGetIcon);
    }

    private void OnGetIcon(Entity<BlinkableComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
        {
            return;
        }

        if (!_blinkingSystem.IsBlind(ent, ent.Comp) && !_eyeClosing.AreEyesClosed(ent.Owner))
        {
            return;
        }

        var iconPrototype = _prototypeManager.Index(ent.Comp.ClosedEyeIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
