using System.Numerics;
using Content.Server.Audio;
using Content.Server.Defusable.WireActions;
using Content.Server.Interaction;
using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp096;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly SharedBlinkingSystem _blinkingSystem = default!;
    [Dependency] private readonly OccluderSystem _occluderSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;


    private ISawmill _sawmill = Logger.GetSawmill("scp096");
    private static string SleepStatusEffectKey = "ForcedSleep";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<Scp096Component, MobStateChangedEvent>(OnSpcStateChanged);
        SubscribeLocalEvent<Scp096Component, StatusEffectEndedEvent>(OnStatusEffectEnded);

        InitTargets();
    }

    private void OnStatusEffectEnded(Entity<Scp096Component> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != SleepStatusEffectKey)
        {
            return;
        }

        ent.Comp.Pacified = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateTargets(frameTime);

        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var entUid, out var scpComponent))
        {
            if (scpComponent.Pacified)
            {
                continue;
            }

            if (scpComponent.InRageMode)
            {
                scpComponent.RageAcc += frameTime;

                if (scpComponent.RageAcc > scpComponent.RageTime)
                {
                    RemoveAllTargets(new Entity<Scp096Component>(entUid, scpComponent));
                    continue;
                }
            }

            if (!CanBeAggro(new Entity<Scp096Component>(entUid, scpComponent)))
            {
                continue;
            }

            FindTargets(new Entity<Scp096Component>(entUid, scpComponent));
        }
    }

    private void AddTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid)
    {
        scpEntity.Comp.Targets.Add(targetUid);

        var scpTarget = EnsureComp<Scp096TargetComponent>(targetUid);
        scpTarget.TargetedBy.Add(scpEntity);

        if (!scpEntity.Comp.InRageMode)
        {
            MakeAngry(scpEntity);
        }

        Dirty(targetUid, scpTarget);
        Dirty(scpEntity);
    }

    private void RemoveTarget(Entity<Scp096Component> scpEntity, Entity<Scp096TargetComponent?> targetEntity, bool removeComponent = true)
    {
        if (!Resolve(targetEntity, ref targetEntity.Comp))
        {
            return;
        }

        scpEntity.Comp.Targets.Remove(targetEntity);
        targetEntity.Comp.TargetedBy.Remove(scpEntity);

        if (targetEntity.Comp.TargetedBy.Count == 0 && removeComponent)
        {
            RemComp<Scp096TargetComponent>(targetEntity);
        }

        if (scpEntity.Comp.Targets.Count == 0)
        {
            Pacify(scpEntity);
        }

        Dirty(scpEntity);
        Dirty(targetEntity);
    }

    private void RemoveAllTargets(Entity<Scp096Component> scpEntity)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var targetUid, out _))
        {
            RemoveTarget(scpEntity, targetUid);
        }
    }

    private void OnSpcStateChanged(Entity<Scp096Component> ent, ref MobStateChangedEvent args)
    {
        if (!_mobStateSystem.IsIncapacitated(ent))
        {
            return;
        }

        RemoveAllTargets(ent);
    }

    private bool CanBeAggro(Entity<Scp096Component> entity)
    {
        if (_mobStateSystem.IsIncapacitated(entity)
            || Comp<BlindableComponent>(entity).IsBlind)
        {
            return false;
        }

        return true;
    }

    protected override void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity)
    {
        base.HandleDoorCollision(scpEntity, doorEntity);

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

    private void FindTargets(Entity<Scp096Component> scpEntity)
    {
        var xform = Transform(scpEntity);
        var query =  _entityLookupSystem.GetEntitiesInRange<BlinkableComponent>(xform.Coordinates, scpEntity.Comp.AgroDistance);

        foreach (var targetUid in query)
        {
            if (!IsValidTarget(scpEntity, targetUid))
            {
                continue;
            }

            AddTarget(scpEntity, targetUid);
        }
    }

    private bool IsValidTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid)
    {
        if (!TryComp<BlinkableComponent>(targetUid, out var blinkableComponent) ||
            !TryComp<BlindableComponent>(targetUid, out var blindableComponent))
        {
            return false;
        }

        var targetXform = Transform(targetUid);

        return !_blinkingSystem.IsBlind(targetUid, blinkableComponent) &&
               !blindableComponent.IsBlind &&
               IsInRange(scpEntity.Owner, targetUid, targetXform, scpEntity.Comp.AgroDistance) &&
               IsWithinViewAngle(scpEntity.Owner, targetUid, scpEntity.Comp.ArgoAngle);
    }

    private bool IsInRange(EntityUid scpEntity, EntityUid targetEntity, TransformComponent targetXform, float range)
    {
        return _interactionSystem.InRangeUnobstructed(scpEntity, targetEntity, targetXform.Coordinates, targetXform.LocalRotation, range);
    }

    private bool IsWithinViewAngle(EntityUid scpEntity, EntityUid targetEntity, float maxAngle)
    {
        return FindAngleBetween(scpEntity, targetEntity) <= maxAngle;
    }

    private float FindAngleBetween(Entity<TransformComponent?> scp, Entity<TransformComponent?> target)
    {
        if(!Resolve<TransformComponent>(scp, ref scp.Comp)
           ||!Resolve<TransformComponent>(target, ref target.Comp))
        {
            return float.MaxValue;
        }

        var toEntity = (scp.Comp.Coordinates - target.Comp.Coordinates).Position.Normalized();

        var dotProduct = Vector2.Dot(target.Comp.LocalRotation.ToWorldVec(), toEntity);
        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
    }

    private void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var entityUid, out var targetComponent))
        {
            targetComponent.TargetedBy.Remove(ent.Owner);

            if (targetComponent.TargetedBy.Count == 0)
            {
                RemComp<Scp096TargetComponent>(entityUid);
            }
        }
    }

    private void Pacify(Entity<Scp096Component> scpEntity)
    {
        EnsureComp<PacifiedComponent>(scpEntity);

        scpEntity.Comp.InRageMode = false;
        scpEntity.Comp.Pacified = true;
        scpEntity.Comp.RageAcc = 0f;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.CrySound);
        _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(scpEntity, SleepStatusEffectKey, TimeSpan.FromSeconds(30.0f), false);

        RefreshSpeedModifiers(scpEntity);
    }

    private void MakeAngry(Entity<Scp096Component> scpEntity)
    {
        RemComp<PacifiedComponent>(scpEntity);

        scpEntity.Comp.InRageMode = true;
        scpEntity.Comp.Pacified = false;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.RageSound);

        RefreshSpeedModifiers(scpEntity);
    }

    private void RefreshSpeedModifiers(Entity<Scp096Component> scpEntity)
    {
        var newSpeed = scpEntity.Comp.InRageMode ? 8.0f : 1.5f;
        _speedModifierSystem.ChangeBaseSpeed(scpEntity, newSpeed, newSpeed, 20.0f);
    }
}
