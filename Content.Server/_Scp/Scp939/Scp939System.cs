﻿using Content.Server.Actions;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Scp.Scp939;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System : EntitySystem
{
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeActions();

        SubscribeLocalEvent<Scp939Component, ComponentInit>(OnInit);

        SubscribeLocalEvent<Scp939Component, SleepStateChangedEvent>(OnSleepChanged);
        SubscribeLocalEvent<Scp939Component, MobStateChangedEvent>(OnMobStateChanged);


        InitializeVisibility();
    }

    private void OnMobStateChanged(Entity<Scp939Component> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        TrySleep(ent, 360f);
    }

    private void OnSleepChanged(Entity<Scp939Component> ent, ref SleepStateChangedEvent args)
    {
        _appearanceSystem.SetData(ent, Scp939Visuals.Sleeping, args.FellAsleep);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Все 939, что спят
        var querySleeping = EntityQueryEnumerator<Scp939Component, SleepingComponent>();

        // Обработка лечения 939 во сне
        while (querySleeping.MoveNext(out var uid, out var scp939Component, out _))
        {
            _damageableSystem.TryChangeDamage(uid, scp939Component.HibernationHealingRate * frameTime);
        }

        // Просто все 939
        var querySimple = EntityQueryEnumerator<Scp939Component>();

        // Обработка плохого зрения 939
        while (querySimple.MoveNext(out var uid, out var scp939Component))
        {
            if (!scp939Component.PoorEyesight)
                continue;

            if (scp939Component.PoorEyesightTimeStart == null)
                continue;

            var timeDifference = _timing.CurTime - scp939Component.PoorEyesightTimeStart.Value;

            if (timeDifference > TimeSpan.FromSeconds(scp939Component.PoorEyesightTime))
            {
                scp939Component.PoorEyesight = false;
                scp939Component.PoorEyesightTimeStart = null;

                Dirty(uid, scp939Component);
            }
        }
    }

    private void OnInit(Entity<Scp939Component> ent, ref ComponentInit args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actionsSystem.AddAction(ent, action);
        }
    }
}
