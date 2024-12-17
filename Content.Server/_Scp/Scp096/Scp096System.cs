﻿using System.Linq;
using System.Numerics;
using Content.Server.Audio;
using Content.Server.Defusable.WireActions;
using Content.Server.Interaction;
using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp096;
using Content.Shared._Scp.Scp096.Mask;
using Content.Shared._Scp.Scp096.Protection;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly SharedBlinkingSystem _blinkingSystem = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly Scp096MaskSystem _scp096Mask = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ISawmill _sawmill = Logger.GetSawmill("scp096");
    private const string SleepStatusEffectKey = "ForcedSleep";

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
            return;

        ent.Comp.Pacified = false;

        Dirty(ent);
    }

    protected override void UpdateScp096(Entity<Scp096Component> scpEntity)
    {
        base.UpdateScp096(scpEntity);

        if (scpEntity.Comp.Pacified)
            return;

        if (!CanBeAggro(scpEntity))
            return;

        FindTargets(scpEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTargets(frameTime);
    }

    protected override void OnRageTimeExceeded(Entity<Scp096Component> scpEntity)
    {
        RemoveAllTargets(scpEntity);
    }

    public bool TryAddTarget(EntityUid targetUid, bool ignoreDistance = false, bool ignoreAngle = false)
    {
        var query = EntityQuery<Scp096Component>().ToHashSet();

        if (query.Count == 0)
            return false;

        var scpEntity = (query.First().Owner, query.First());

        return TryAddTarget(scpEntity, targetUid, ignoreDistance, ignoreAngle);
    }

    public bool TryAddTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid, bool ignoreDistance = false, bool ignoreAngle = false)
    {
        if (!IsValidTarget(scpEntity, targetUid, ignoreDistance, ignoreAngle))
            return false;

        if (!CanBeAggro(scpEntity))
            return false;

        if (HasComp<Scp096ProtectionComponent>(targetUid))
            return false;

        AddTarget(scpEntity, targetUid);

        return true;
    }

    private void AddTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid)
    {
        scpEntity.Comp.Targets.Add(targetUid);

        var scpTarget = EnsureComp<Scp096TargetComponent>(targetUid);
        scpTarget.TargetedBy.Add(scpEntity);

        if (!scpEntity.Comp.InRageMode)
            MakeAngry(scpEntity);

        Dirty(targetUid, scpTarget);
        Dirty(scpEntity);
    }

    private void RemoveTarget(Entity<Scp096Component> scpEntity, Entity<Scp096TargetComponent?> targetEntity, bool removeComponent = true)
    {
        if (!Resolve(targetEntity, ref targetEntity.Comp))
            return;

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

        Dirty(targetEntity);
        Dirty(scpEntity);
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
        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
    }

    private bool CanBeAggro(Entity<Scp096Component> entity)
    {
        if (_mobStateSystem.IsIncapacitated(entity) || Comp<BlindableComponent>(entity).IsBlind)
            return false;

        // В маске мы мирные
        if (_scp096Mask.TryGetScp096Mask(entity, out _))
            return false;

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
            TryAddTarget(scpEntity, targetUid);
        }
    }

    private bool IsValidTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid, bool ignoreDistance = false, bool ignoreAngle = false)
    {
        if (!TryComp<BlinkableComponent>(targetUid, out var blinkableComponent) ||
            !TryComp<BlindableComponent>(targetUid, out var blindableComponent))
        {
            return false;
        }

        var targetXform = Transform(targetUid);

        return !_blinkingSystem.IsBlind(targetUid, blinkableComponent) &&
               !blindableComponent.IsBlind &&
               (IsInRange(scpEntity.Owner, targetUid, targetXform, scpEntity.Comp.AgroDistance) || ignoreDistance) &&
               (IsWithinViewAngle(scpEntity.Owner, targetUid, scpEntity.Comp.ArgoAngle) || ignoreAngle);
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
        if (!Resolve<TransformComponent>(scp, ref scp.Comp)
           ||!Resolve<TransformComponent>(target, ref target.Comp))
        {
            return float.MaxValue;
        }

        var scpWorldPosition = _transformSystem.GetMapCoordinates(scp.Owner);
        var targetWorldPosition = _transformSystem.GetMapCoordinates(target.Owner);

        var toEntity = (scpWorldPosition.Position - targetWorldPosition.Position).Normalized();

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
        scpEntity.Comp.RageStartTime = null;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.CrySound);
        _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(scpEntity, SleepStatusEffectKey, TimeSpan.FromSeconds(scpEntity.Comp.PacifiedTime), false);

        RefreshSpeedModifiers(scpEntity);
    }

    private void MakeAngry(Entity<Scp096Component> scpEntity)
    {
        RemComp<PacifiedComponent>(scpEntity);

        scpEntity.Comp.InRageMode = true;
        scpEntity.Comp.Pacified = false;
        scpEntity.Comp.RageStartTime = _gameTiming.CurTime;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.RageSound);

        RefreshSpeedModifiers(scpEntity);
    }

    private void RefreshSpeedModifiers(Entity<Scp096Component> scpEntity)
    {
        var newSpeed = scpEntity.Comp.InRageMode ? 8.0f : 1.5f;
        _speedModifierSystem.ChangeBaseSpeed(scpEntity, newSpeed, newSpeed, 20.0f);
    }
}
