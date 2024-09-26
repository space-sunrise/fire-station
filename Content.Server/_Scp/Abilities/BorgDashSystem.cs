﻿using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.Stunnable;
using Content.Shared._Scp.Abilities;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Scp.Abilities;

/// <summary>
/// This handles...
/// </summary>
public sealed class BorgDashSystem : SharedBorgDashSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("borg.dash");

        SubscribeLocalEvent<BorgDashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BorgDashComponent, BorgDashActionEvent>(OnDash);
        SubscribeLocalEvent<BorgDashComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgDashComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<BorgDashComponent, LandEvent>(OnLanded);
        SubscribeLocalEvent<BorgDashComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<BorgDashComponent, EntParentChangedMessage>(OnChangedParent);
    }

    private void OnInit(EntityUid uid, BorgDashComponent component, ComponentInit args)
    {
        component.DashActionUid ??= _actions.AddAction(uid, component.DashActionId);
    }

    private void OnShutdown(EntityUid uid, BorgDashComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(component.DashActionUid);
    }

    private void OnLanded(EntityUid uid, BorgDashComponent component, LandEvent args)
    {
        if (!component.IsDashing)
            return;
        component.IsDashing = false;
        var ev = new BorgLandedEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev);
    }

    private void OnChangedParent(EntityUid uid, BorgDashComponent component, EntParentChangedMessage args)
    {
        if (!HasComp<MapComponent>(args.OldParent))
            return;
        if (!component.IsDashing)
            return;
        component.IsDashing = false;
        var ev = new BorgLandedEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev);
    }

    private void OnStartCollide(EntityUid uid, BorgDashComponent component, StartCollideEvent args)
    {
        if ((args.OtherBody.BodyType == BodyType.Static || args.OtherBody.BodyType == BodyType.Dynamic) &&
            args.OtherBody.CollisionLayer != 20 &&
            args.OtherBody.CanCollide)
        {
            if (!component.IsDashing)
                return;
            component.IsDashing = false;
            var ev = new BorgLandedEvent(GetNetEntity(uid));
            RaiseNetworkEvent(ev);
        }
    }

    private void OnThrowHit(EntityUid uid, BorgDashComponent component, ThrowDoHitEvent args)
    {
        if (!component.IsDashing)
            return;

        if (HasComp<BorgChassisComponent>(args.Target))  // Хз, впринципе можно это закомментировать
            return;

        if (!_mobState.IsAlive(args.Target)) // Мертвых и критовых незя
            return;

        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(1), false);

        var xform = Transform(uid);
        var targetXform = Transform(args.Target);
        var mapCoords = targetXform.Coordinates.ToMap(EntityManager, _transform);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        direction.Normalize();
        direction = -(direction);

        _throwing.TryThrow(args.Target, direction, 10f, args.Target, 15f);

        if (!HasComp<ZombieComponent>(uid))
            _damageable.TryChangeDamage(args.Target, component.Damage);
        else
            _damageable.TryChangeDamage(args.Target, component.ZombieDamage);

        _audio.PlayPvs(component.ThrowHitSound, uid);
    }

    private void OnDash(EntityUid uid, BorgDashComponent component, BorgDashActionEvent args)
    {
        if (TryComp(uid, out BorgResistComponent? borgResistComponent))
        {
            if (borgResistComponent.Enabled)
                return;  // Нельзя прыгать если включен щит
        }

        if (args.Handled)
            return;
        args.Handled = true;

        if (!_powerCell.TryUseCharge(uid, component.DashChargeDrop))
        {
            _popup.PopupEntity(Loc.GetString("droid-no-charge", ("name", MetaData(args.Action).EntityName)), uid, uid, PopupType.LargeCaution);
            return;
        }

        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(EntityManager, _transform);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        if (direction.Length() > component.MaxDash)
        {
            direction = direction.Normalized() * component.MaxDash;
        }

        _throwing.TryThrow(uid, direction, component.DashSpeed, uid, component.DashSpeed);
        component.IsDashing = true;

        _audio.PlayPvs(component.DashSound, uid);

        var ev = new BorgThrownEvent(GetNetEntity(uid));
        RaiseNetworkEvent(ev);
    }
}
