using System.Linq;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp914;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp914;

public sealed class Scp914System : SharedScp914System
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly EntityStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp914Component, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<Scp914Component, InteractHandEvent>(OnInteractHand);

        Subs.BuiEvents<Scp914Component>(Scp914UiKey.Key,
            subscriber =>
            {
                subscriber.Event<Scp914ChangeModeRequestMessage>(OnChangeModeRequest);
                subscriber.Event<Scp914StartCycleMessage>(OnStartCycleRequest);
            });
    }

    private void UpdateState(Entity<Scp914Component> entity)
    {
        var state = new Scp914BuiState(entity.Comp.CurrentMode, entity.Comp.Active);
        _userInterfaceSystem.SetUiState(entity.Owner, Scp914UiKey.Key, state);
    }

    private void OnInteractHand(Entity<Scp914Component> ent, ref InteractHandEvent args)
    {
        UpdateState(ent);
        _userInterfaceSystem.TryOpenUi(ent.Owner, Scp914UiKey.Key, args.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp914Component>();

        while (query.MoveNext(out var entityUid, out var scp914Component))
        {
            if (!scp914Component.Active || _gameTiming.CurTime <= scp914Component.UpgradeTimeEnd)
                continue;

            var scpEntity = new Entity<Scp914Component>(entityUid, scp914Component);

            scp914Component.Active = false;
            ToggleLock(scpEntity, false);

            scp914Component.NextUpgradeTime = _gameTiming.CurTime + scp914Component.UpgradeCooldown;
        }
    }

    private void OnMapInit(Entity<Scp914Component> machineEntity, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<TransformComponent, Scp914ContainerComponent>();
        var machineXform = Transform(machineEntity);

        while (query.MoveNext(out var containerEntity, out var containerXform, out var containerComponent))
        {
            if (machineXform.MapID != containerXform.MapID ||
                !_transformSystem.InRange(machineEntity.Owner, containerEntity, 8f))
            {
                continue;
            }

            if (containerComponent.ContainerType == Scp914ContainerType.Input)
            {
                machineEntity.Comp.InputContainer = containerEntity;
            }
            else
            {
                machineEntity.Comp.OutputContainer = containerEntity;
            }
        }
    }

    private void OnChangeModeRequest(Entity<Scp914Component> ent, ref Scp914ChangeModeRequestMessage args)
    {
        if (ent.Comp.Active)
        {
            return;
        }

        if (ent.Comp.NextChangeCycleTime > _gameTiming.CurTime)
        {
            return;
        }

        var newMode = CycleMod(ent.Comp.CurrentMode, args.Direction);

        ent.Comp.CurrentMode = newMode;
        ent.Comp.NextChangeCycleTime = _gameTiming.CurTime + ent.Comp.CycleCooldown;

        _audioSystem.PlayPvs(ent.Comp.ClackSound, ent);

        UpdateState(ent);
        Dirty(ent);
    }

    private void OnStartCycleRequest(Entity<Scp914Component> ent, ref Scp914StartCycleMessage args)
    {
        if (ent.Comp.Active)
        {
            return;
        }

        if (ent.Comp.NextUpgradeTime > _gameTiming.CurTime)
        {
            var message = Loc.GetString("scp914-cycle-timeout");
            _popupSystem.PopupEntity(message, ent, PopupType.LargeCaution);
            return;
        }

        _audioSystem.PlayPvs(ent.Comp.ClackSound, ent, AudioParams.Default.WithPitchScale(-10f).WithVariation(0.1f));
        _audioSystem.PlayPvs(ent.Comp.RefineSound, ent);

        ent.Comp.Active = true;
        ent.Comp.UpgradeTimeEnd = _gameTiming.CurTime + ent.Comp.UpgradeDuration;

        ToggleLock(ent, true);
        ProcessUpgrades(ent);
        UpdateState(ent);

        Dirty(ent);
    }

    private void ToggleLock(Entity<Scp914Component> ent, bool lockState)
    {
        var inputContainer = ent.Comp.InputContainer;
        var outputContainer = ent.Comp.OutputContainer;

        if (lockState)
        {
            _storageSystem.CloseStorage(inputContainer);
            _storageSystem.CloseStorage(outputContainer);
        }
        else
        {
            _storageSystem.OpenStorage(inputContainer);
            _storageSystem.OpenStorage(outputContainer);
        }
    }

    private void ProcessUpgrades(Entity<Scp914Component> machineEntity)
    {
        var inputContainer = GetContainer(machineEntity, Scp914ContainerType.Input);
        var outputContainer = GetContainer(machineEntity, Scp914ContainerType.Output);

        var machineMode = machineEntity.Comp.CurrentMode;

        foreach (var containedEntity in inputContainer.ContainedEntities.ToList())
        {
            EntityUid? upgradedEntity = containedEntity;

            if (TryComp<Scp914UpgradableComponent>(containedEntity, out var upgradableComponent))
            {
                var upgradableEntity = new Entity<Scp914UpgradableComponent>(containedEntity, upgradableComponent);

                UpgradeItem(upgradableEntity, machineMode, ref upgradedEntity);
            }

            if (upgradedEntity.HasValue)
            {
                _containerSystem.Insert(upgradedEntity.Value, outputContainer, force: true);
            }
        }
    }

    private void UpgradeItem(Entity<Scp914UpgradableComponent> upgradableEntity, Scp914Mode machineMode, ref EntityUid? entity)
    {
        var options = upgradableEntity.Comp.UpgradeOptions;

        if (!options.TryGetValue(machineMode, out var upgradeOptions))
        {
            return;
        }

        var randomValue = _random.NextFloat();
        var cumulativeProbability = 0f;

        foreach (var option in upgradeOptions)
        {
            cumulativeProbability += option.Chance;

            if (randomValue > cumulativeProbability)
            {
                continue;
            }

            if (!option.Item.HasValue)
            {
                Del(upgradableEntity);
                entity = null;

                break;
            }

            Del(upgradableEntity);

            entity = Spawn(option.Item);
            break;
        }
    }

    private BaseContainer GetContainer(Entity<Scp914Component> machineEntity, Scp914ContainerType containerType)
    {
        BaseContainer container;

        if (containerType == Scp914ContainerType.Input)
        {
            container = Comp<EntityStorageComponent>(machineEntity.Comp.InputContainer).Contents;
        }
        else
        {
            container = Comp<EntityStorageComponent>(machineEntity.Comp.OutputContainer).Contents;
        }

        return container;
    }
}
