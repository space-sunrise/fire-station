using System.Linq;
using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Scp096.Mask;
using Content.Shared._Scp.Scp096.Protection;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinkingSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly Scp096MaskSystem _scp096Mask = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private ISawmill _sawmill = Logger.GetSawmill("scp096");
    private const string SleepStatusEffectKey = "ForcedSleep";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, AttemptPacifiedAttackEvent>(OnPacifiedAttackAttempt);
        SubscribeLocalEvent<Scp096Component, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<Scp096Component, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<Scp096Component, MobStateChangedEvent>(OnSpcStateChanged);

        SubscribeLocalEvent<Scp096Component, ComponentShutdown>(OnShutdown);
    }

    #region Updater

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var scpUid, out var scp096Component))
        {
            var scpEntity = new Entity<Scp096Component>(scpUid, scp096Component);
            UpdateScp096(scpEntity);
            UpdateVisualState(scpEntity);
        }

        UpdateTargets(frameTime);
    }

    private void UpdateScp096(Entity<Scp096Component> scpEntity)
    {
        if (!CanBeAggro(scpEntity))
            return;

        FindTargets(scpEntity);

        if (!scpEntity.Comp.InRageMode)
            return;

        if (!scpEntity.Comp.RageStartTime.HasValue)
            return;

        var currentTime = _gameTiming.CurTime;
        var elapsedTime = currentTime - scpEntity.Comp.RageStartTime.Value;

        if (elapsedTime.TotalSeconds > scpEntity.Comp.RageDuration)
        {
            OnRageTimeExceeded(scpEntity);
        }
    }

    #endregion

    #region Event handlers

    private void OnSpcStateChanged(Entity<Scp096Component> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
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

    private void OnPacifiedAttackAttempt(Entity<Scp096Component> ent, ref AttemptPacifiedAttackEvent args)
    {
        args.Reason = Loc.GetString("scp096-non-argo-attack-attempt");
        args.Cancelled = true;
    }

    private void OnPullAttempt(Entity<Scp096Component> ent, ref PullAttemptEvent args)
    {
        if (HasComp<PacifiedComponent>(ent))
            args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        if (!args.Target.HasValue)
            return;

        if (!TryComp<Scp096TargetComponent>(args.Target.Value, out var targetComponent)
            || !targetComponent.TargetedBy.Contains(ent.Owner))
        {
            args.Cancel();
        }
    }

    #endregion

    #region Targets

    public bool TryAddTarget(EntityUid targetUid, bool ignoreDistance = false, bool ignoreAngle = false)
    {
        var query = EntityQuery<Scp096Component>().ToHashSet();

        if (query.Count == 0)
            return false;

        var scpEntity = (query.First().Owner, query.First());

        return TryAddTarget(scpEntity, targetUid, ignoreDistance, ignoreAngle);
    }

    private bool TryAddTarget(Entity<Scp096Component> scpEntity, EntityUid targetUid, bool ignoreDistance = false, bool ignoreAngle = false)
    {
        if (!IsValidTarget(scpEntity, targetUid, ignoreDistance, ignoreAngle))
            return false;

        if (!CanBeAggro(scpEntity))
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
        // Если не умер, не в крите и вообще чувствует себя хорошо
        if (_mobStateSystem.IsIncapacitated(targetUid))
            return false;

        // Если таргет не имеет защиты
        if (HasComp<Scp096ProtectionComponent>(targetUid))
            return false;

        // Если таргет не имеет возможности моргать
        if (!TryComp<BlinkableComponent>(targetUid, out var blinkableComponent))
            return false;

        // Если таргет не может быть слепым
        if (!TryComp<BlindableComponent>(targetUid, out var blindableComponent))
            return false;

        // Если таргет слепой
        if (_blinkingSystem.IsBlind(targetUid, blinkableComponent))
            return false;

        // Если таргет закрыл глаза
        if (blindableComponent.IsBlind)
            return false;

        var targetXform = Transform(targetUid);

        // Если таргет не в зоне действия агра и зона действия не игнорируется
        if (!IsInRange(scpEntity.Owner, targetUid, targetXform, scpEntity.Comp.AgroDistance) && !ignoreDistance)
            return false;

        // Если таргет не смотрит на 096 под определенным углом и требуемый угол не игнорируется
        if (!IsWithinViewAngle(scpEntity.Owner, targetUid, scpEntity.Comp.ArgoAngle) && !ignoreAngle)
            return false;

        // Тогда все заебись и таргет подходит
        return true;
    }

    #endregion

    private void OnRageTimeExceeded(Entity<Scp096Component> scpEntity)
    {
        RemoveAllTargets(scpEntity);
    }

    #region Helpers

    private bool CanBeAggro(Entity<Scp096Component> entity)
    {
        if (HasComp<SleepingComponent>(entity))
            return false;

        if (_mobStateSystem.IsIncapacitated(entity) || Comp<BlindableComponent>(entity).IsBlind)
            return false;

        // В маске мы мирные
        if (_scp096Mask.TryGetScp096Mask(entity, out _))
            return false;

        return true;
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
        if (!Resolve<TransformComponent>(scp, ref scp.Comp))
            return float.MaxValue;

        if (!Resolve<TransformComponent>(target, ref target.Comp))
            return float.MaxValue;

        var scpWorldPosition = _transformSystem.GetMapCoordinates(scp.Owner);
        var targetWorldPosition = _transformSystem.GetMapCoordinates(target.Owner);

        var toEntity = (scpWorldPosition.Position - targetWorldPosition.Position).Normalized();

        var dotProduct = Vector2.Dot(target.Comp.LocalRotation.ToWorldVec(), toEntity);
        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
    }

    private void RefreshSpeedModifiers(Entity<Scp096Component> scpEntity)
    {
        var newSpeed = scpEntity.Comp.InRageMode ? scpEntity.Comp.RageSpeed : scpEntity.Comp.BaseSpeed;
        _speedModifierSystem.ChangeBaseSpeed(scpEntity, newSpeed, newSpeed, 20.0f);
    }

    #endregion

    #region Rage handlers

    private void Pacify(Entity<Scp096Component> scpEntity)
    {
        EnsureComp<PacifiedComponent>(scpEntity);

        scpEntity.Comp.InRageMode = false;
        scpEntity.Comp.RageStartTime = null;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.CrySound);
        _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(scpEntity, SleepStatusEffectKey, TimeSpan.FromSeconds(scpEntity.Comp.PacifiedTime), true);

        RefreshSpeedModifiers(scpEntity);
    }

    private void MakeAngry(Entity<Scp096Component> scpEntity)
    {
        RemComp<PacifiedComponent>(scpEntity);

        scpEntity.Comp.InRageMode = true;
        scpEntity.Comp.RageStartTime = _gameTiming.CurTime;

        _ambientSoundSystem.SetSound(scpEntity, scpEntity.Comp.RageSound);

        RefreshSpeedModifiers(scpEntity);
    }

    #endregion

    #region Rage-mode door handling

    private void OnCollide(Entity<Scp096Component> ent, ref StartCollideEvent args)
    {
        if (!TryComp<DoorComponent>(args.OtherEntity, out var doorComponent))
            return;

        HandleDoorCollision(ent, new Entity<DoorComponent>(args.OtherEntity, doorComponent));
    }

    protected virtual void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity)
    {
        if (!scpEntity.Comp.InRageMode)
            return;

        _doorSystem.StartOpening(doorEntity);
    }

    #endregion

    private void UpdateVisualState(Entity<Scp096Component> scpEntity)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        Scp096VisualsState state;
        var physicsComponent = Comp<PhysicsComponent>(scpEntity);
        var moving = physicsComponent.LinearVelocity.Length() > 0;

        if (_mobStateSystem.IsCritical(scpEntity) || HasComp<SleepingComponent>(scpEntity))
        {
            state = Scp096VisualsState.Dead;
        }
        else if (scpEntity.Comp.InRageMode)
        {
            state = moving ? Scp096VisualsState.Running : Scp096VisualsState.IdleAgro;
        }
        else
        {
            state = moving ? Scp096VisualsState.Walking : Scp096VisualsState.Idle;
        }

        _appearanceSystem.SetData(scpEntity, Scp096Visuals.Visuals, state);
    }

}
