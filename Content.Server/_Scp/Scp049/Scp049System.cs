using Content.Server.Actions;
using Content.Shared._Scp.Scp049;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.Map;
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
        var mobStateComponent = Comp<MobStateComponent>(args.Target!.Value);
        var mobStateEntity = new Entity<MobStateComponent>(args.Target.Value, mobStateComponent);

        scpEntity.Comp.NextTool = _random.Pick(scpEntity.Comp.SurgeryTools);

        // TODO: Норм строки
        if (!TryMakeMinion(mobStateEntity, scpEntity))
            _popupSystem.PopupEntity("Тело не подлежит лечению", mobStateEntity, scpEntity);
    }


    private void OnStartup(Entity<Scp049Component> ent, ref ComponentStartup args)
    {
        foreach (var action in ent.Comp.Scp049Actions)
        {
            _actionsSystem.AddAction(ent, action);
        }

        var backPack = Spawn("ClothingBackpackScp049", MapCoordinates.Nullspace);
        _inventorySystem.TryEquip(ent, backPack, "back", true, true);

        ent.Comp.NextTool = _random.Pick(ent.Comp.SurgeryTools);
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
