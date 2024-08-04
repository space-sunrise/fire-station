using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared._Scp.Abilities;
using Content.Shared.Actions;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Reflect;
using Microsoft.CodeAnalysis;
using Robust.Server.Audio;
using Robust.Shared.Audio.Systems;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Abilities;

public sealed class BorgResistSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgResistComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BorgResistComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgResistComponent, BorgResistanceActionEvent>(OnResist);
    }

    private void OnInit(EntityUid uid, BorgResistComponent component, ComponentInit args)
    {
        component.ResistActionUid ??= _actions.AddAction(uid, component.ResistActionId);
    }

    private void OnShutdown(EntityUid uid, BorgResistComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(component.ResistActionUid);
    }

    private void OnResist(EntityUid uid, BorgResistComponent component, BorgResistanceActionEvent args)
    {
        if (!TryComp(uid, out MovementSpeedModifierComponent? modifierComponent) ||
            !TryComp(uid, out ReflectComponent? reflectComponent))
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        if (component.Enabled)
        {
            DisableShield(uid, component, modifierComponent, reflectComponent);
        }
        else
        {
            if (!_powerCell.HasCharge(uid, component.DrainCharge))
            {
                _popup.PopupEntity(Loc.GetString("droid-no-charge", ("name", MetaData(args.Action).EntityName)), uid, uid, PopupType.LargeCaution);
                return;
            }
            EnableShield(uid, component, modifierComponent, reflectComponent);
            DrainCharge(uid, component, TimeSpan.FromSeconds(1), component.DrainCharge);
        }
    }

    private void EnableShield(EntityUid uid,
        BorgResistComponent component,
        MovementSpeedModifierComponent modifierComponent,
        ReflectComponent reflectComponent)
    {
        _speedModifier.ChangeBaseSpeed(uid,
            modifierComponent.BaseWalkSpeed * component.SpeedModifier,
            modifierComponent.BaseSprintSpeed * component.SpeedModifier,
            modifierComponent.Acceleration);

        reflectComponent.ReflectProb += component.MultReflectionChance;

        var ev = new BorgShieldEnabledEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev);

        component.Enabled = true;

        _audio.PlayPvs(component.SoundActivate, uid, component.SoundActivate.Params);
    }

    private void DisableShield(EntityUid uid,
        BorgResistComponent component,
        MovementSpeedModifierComponent modifierComponent,
        ReflectComponent reflectComponent)
    {

        _speedModifier.ChangeBaseSpeed(uid,
            modifierComponent.BaseWalkSpeed / component.SpeedModifier,
            modifierComponent.BaseSprintSpeed / component.SpeedModifier,
            modifierComponent.Acceleration);

        reflectComponent.ReflectProb -= component.MultReflectionChance;

        var ev = new BorgShieldDisabledEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev);

        component.Enabled = false;

        _audio.PlayPvs(component.SoundDeactivate, uid, component.SoundDeactivate.Params);
    }

    private void DrainCharge(EntityUid uid, BorgResistComponent component, TimeSpan delay, float charge)
    {
        if (!component.Enabled)
            return;

        if (!_powerCell.TryUseCharge(uid, charge))
        {
            if (!TryComp(uid, out MovementSpeedModifierComponent? modifierComponent) ||
                !TryComp(uid, out ReflectComponent? reflectComponent))
                return;

            DisableShield(uid, component, modifierComponent, reflectComponent);
            _popup.PopupEntity("droid-resist-no-charge", uid, uid, PopupType.LargeCaution);

            return;
        }

        Timer.Spawn(delay, () => { DrainCharge(uid, component, delay, charge); });
    }
}
