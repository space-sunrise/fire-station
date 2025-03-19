﻿using Content.Server.Actions;
using Content.Shared._Scp.Scp049;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp049;

public sealed partial class Scp049System : SharedScp049System
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp049Component, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<Scp049Component, ScpResurrectionDoAfterEvent>(OnResurrectDoAfter);

        InitializeActions();
    }

    private void OnResurrectDoAfter(Entity<Scp049Component> scpEntity, ref ScpResurrectionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!args.Target.HasValue)
            return;

        if (!TryComp<MobStateComponent>(args.Target, out var mobStateComponent))
            return;

        var mobStateEntity = new Entity<MobStateComponent>(args.Target.Value, mobStateComponent);

        scpEntity.Comp.NextTool = _random.Pick(scpEntity.Comp.SurgeryTools);
        Dirty(scpEntity);

        if (!TryMakeMinion(mobStateEntity, scpEntity))
        {
            var message = Loc.GetString("scp049-cannot-zombify-entity", ("target", mobStateEntity));
            _popupSystem.PopupEntity(message, mobStateEntity, scpEntity);
        }

        args.Handled = true;
    }

    // TODO: Перенести на другие компоненты
    private void OnStartup(Entity<Scp049Component> ent, ref ComponentStartup args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actionsSystem.AddAction(ent, action);
        }

        var backPack = Spawn("ClothingBackpackScp049");
        _inventorySystem.TryEquip(ent, backPack, "back", true, true);

        ent.Comp.NextTool = _random.Pick(ent.Comp.SurgeryTools);
        Dirty(ent);
    }

    private ZombieComponent BuildZombieComponent(EntityUid target)
    {
        var appearanceComponent = Comp<HumanoidAppearanceComponent>(target);

        var zombieComponent = new ZombieComponent
        {
            SkinColor = appearanceComponent.SkinColor,
            EyeColor = Color.Red,
            StatusIcon = Scp049MinionComponent.Icon
        };

        return zombieComponent;
    }
}
