﻿using Content.Server.Administration.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.NPC.HTN;
using Content.Server.Zombies;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared._Scp.Scp049;
using Content.Shared._Scp.Scp049.Scp049Protection;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._Scp.Scp049;

public sealed partial class Scp049System
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenateSystem = default!;
    [Dependency] private readonly ZombieSystem _zombieSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    private void InitializeActions()
    {
        SubscribeLocalEvent<Scp049Component, Scp049KillLivingBeingAction>(OnKillLeavingBeing);
        SubscribeLocalEvent<Scp049Component, Scp049KillResurrectedAction>(OnKillResurrected);
        SubscribeLocalEvent<Scp049Component, Scp049ResurrectAction>(OnResurrect);
        SubscribeLocalEvent<Scp049Component, Scp049SelfHealAction>(OnSelfHeal);
        SubscribeLocalEvent<Scp049Component, Scp049HealMinionAction>(OnHealMinion);
    }

    private void OnHealMinion(Entity<Scp049Component> ent, ref Scp049HealMinionAction args)
    {
        if (args.Handled)
            return;

        if (!HasComp<Scp049MinionComponent>(args.Target))
            return;

        Heal(args.Target, args.Performer);

        args.Handled = true;
    }

    private void OnSelfHeal(Entity<Scp049Component> ent, ref Scp049SelfHealAction args)
    {
        if (args.Handled)
            return;

        Heal(args.Performer, args.Performer);

        args.Handled = true;
    }

    private void Heal(EntityUid target, EntityUid performer)
    {
        _rejuvenateSystem.PerformRejuvenate(target);

        var targetName = Identity.Name(target, EntityManager);
        var localeMessage = Loc.GetString("scp049-heal-minion", ("target", targetName));
        _popupSystem.PopupEntity(localeMessage, performer, PopupType.Medium);
    }

    private void OnResurrect(Entity<Scp049Component> scpEntity, ref Scp049ResurrectAction args)
    {
        if (args.Handled)
            return;

        var hasTool = false;

        foreach (var heldUid in _handsSystem.EnumerateHeld(scpEntity))
        {
            var metaData = MetaData(heldUid);

            if (metaData.EntityPrototype == null)
                continue;

            if (metaData.EntityPrototype.ID != scpEntity.Comp.NextTool)
                continue;

            hasTool = true;
            break;
        }

        if (!hasTool)
        {
            var message = Loc.GetString("scp049-missing-surgery-tool", ("instrument", Loc.GetEntityData(scpEntity.Comp.NextTool).Name));
            _popupSystem.PopupEntity(message, scpEntity, scpEntity, PopupType.MediumCaution);

            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            scpEntity,
            scpEntity.Comp.ResurrectionTime,
            new ScpResurrectionDoAfterEvent(),
            target: args.Target,
            eventTarget: scpEntity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true
        };

        args.Handled = _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnKillResurrected(Entity<Scp049Component> ent, ref Scp049KillResurrectedAction args)
    {
        if (args.Handled)
            return;

        if (!_zombieSystem.UnZombify(args.Target, args.Target, null))
            return;

        _mobStateSystem.ChangeMobState(args.Target, MobState.Dead);
        RemComp<Scp049MinionComponent>(args.Target);

        var targetName = Identity.Name(args.Target, EntityManager);
        var performerName = Identity.Name(args.Target, EntityManager);

        var localeMessage = Loc.GetString("scp049-touch-action-success",
            ("target", targetName),
            ("performer", performerName));

        _popupSystem.PopupEntity(localeMessage, ent, PopupType.MediumCaution);

        args.Handled = true;
    }

    private void OnKillLeavingBeing(Entity<Scp049Component> ent, ref Scp049KillLivingBeingAction args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (HasComp<ScpComponent>(target))
            return;

        if (_mobStateSystem.IsDead(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("scp049-kill-action-already-dead"), target, ent, PopupType.MediumCaution);
            return;
        }

        if (!_mobStateSystem.HasState(target, MobState.Dead))
        {
            _popupSystem.PopupEntity(Loc.GetString("scp049-kill-action-cant-kill"), target, ent, PopupType.MediumCaution);
            return;
        }

        _mobStateSystem.ChangeMobState(target, MobState.Dead);

        var targetName = Identity.Name(target, EntityManager);
        var performerName = Identity.Name(ent, EntityManager);

        var localeMessage = Loc.GetString("scp049-touch-action-success",
            ("target", targetName),
            ("performer", performerName));

        _popupSystem.PopupEntity(localeMessage, ent, PopupType.MediumCaution);

        args.Handled = true;
    }

    private bool TryMakeMinion(Entity<MobStateComponent> minionEntity, Entity<Scp049Component> scpEntity)
    {
        if (HasComp<Scp049ProtectionComponent>(minionEntity))
            return false;

        if (HasComp<ScpComponent>(minionEntity))
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(minionEntity))
            return false;

        MakeMinion(minionEntity, scpEntity);

        return true;
    }

    private void MakeMinion(Entity<MobStateComponent> minionEntity, Entity<Scp049Component> scpEntity)
    {
        var minionComponent = EnsureComp<Scp049MinionComponent>(minionEntity.Owner);
        EnsureComp<ScpShow049HudComponent>(minionEntity);

        minionComponent.Scp049Owner = scpEntity;
        Dirty(minionEntity.Owner, minionComponent);

        scpEntity.Comp.Minions.Add(minionEntity);

        var zombieComponent = BuildZombieComponent(minionEntity);
        _zombieSystem.ZombifyEntity(minionEntity, zombieComponentOverride: zombieComponent);

        EnsureComp<NonSpreaderZombieComponent>(minionEntity);

        _rejuvenateSystem.PerformRejuvenate(minionEntity);
        _mobStateSystem.ChangeMobState(minionEntity, MobState.Alive);

        RemComp<HTNComponent>(minionEntity);

        TryMakeAGhostRole(minionEntity);
    }

    private void TryMakeAGhostRole(EntityUid minionUid)
    {
        if (_mindSystem.TryGetMind(minionUid, out _, out var mindComponent) &&
            _mindSystem.TryGetSession(mindComponent, out _))
            return;

        var ghostRoleComponent = EnsureComp<GhostRoleComponent>(minionUid);
        ghostRoleComponent.RoleName = Loc.GetString("scp049-ghost-role-name");
        ghostRoleComponent.RoleDescription = Loc.GetString("scp049-ghost-role-description");
        ghostRoleComponent.RoleRules = Loc.GetString("scp049-ghost-role-rules");

        EnsureComp<GhostTakeoverAvailableComponent>(minionUid);
    }
}
