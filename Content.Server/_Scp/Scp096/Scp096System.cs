using Content.Server._Scp.Misc.EmitSoundRandomly;
using Content.Server.Defusable.WireActions;
using Content.Server.Doors.Systems;
using Content.Server.Power;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Wires;
using Content.Shared._Scp.Scp096;
using Content.Shared.Bed.Sleep;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Content.Shared.Wires;
using Robust.Server.Audio;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly SharedWiresSystem _wiresSystem = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private readonly SoundSpecifier _storageOpenSound = new SoundCollectionSpecifier("MetalBreak");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, BeforeRandomlyEmittingSoundEvent>(OnEmitSoundRandomly);

        InitTargets();
    }

    private void OnEmitSoundRandomly(Entity<Scp096Component> ent, ref BeforeRandomlyEmittingSoundEvent args)
    {
        if (ent.Comp.InRageMode || HasComp<SleepingComponent>(ent))
            args.Cancel();
    }

    protected override void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        base.OnAttackAttempt(ent, ref args);

        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        // randomly opens some lockers and such.
        if (TryComp<EntityStorageComponent>(target, out var entityStorageComponent) && !entityStorageComponent.Open)
        {
            _lock.TryUnlock(target, ent);
            _entityStorage.OpenStorage(target, entityStorageComponent);
            _audioSystem.PlayPvs(_storageOpenSound, ent);
        }
    }

    // TODO: Переделать это под отдельный компонент, который будет выдаваться и убираться
    protected override void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity)
    {
        base.HandleDoorCollision(scpEntity, doorEntity);

        if (!scpEntity.Comp.InRageMode)
            return;

        _doorSystem.StartOpening(doorEntity);

        if (TryComp<DoorBoltComponent>(doorEntity, out var doorBoltComponent))
        {
            _doorSystem.SetBoltsDown(new(doorEntity, doorBoltComponent), true);
        }

        if (!TryComp<WiresComponent>(doorEntity, out var wiresComponent))
            return;

        if (TryComp<WiresPanelComponent>(doorEntity, out var wiresPanelComponent))
        {
            _wiresSystem.TogglePanel(doorEntity, wiresPanelComponent, true);
        }

        foreach (var x in wiresComponent.WiresList)
        {
            if (x.Action is PowerWireAction or BoltWireAction) //Always cut this wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
            else if (_random.Prob(scpEntity.Comp.WireCutChance)) // randomly cut other wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
        }

        _audioSystem.PlayPvs(scpEntity.Comp.DoorSmashSoundCollection, doorEntity);
    }

    protected override void AddTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid)
    {
        base.AddTarget(scpEntity, targetUid);

        _pvsOverride.AddGlobalOverride(targetUid);
    }

    protected override void RemoveTarget(Entity<Scp096Component> scpEntity, Entity<Scp096TargetComponent?> targetEntity, bool removeComponent = true)
    {
        base.RemoveTarget(scpEntity, targetEntity, removeComponent);

        _pvsOverride.RemoveGlobalOverride(targetEntity);
    }


}
