using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Blood;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Scp096.Protection;
using Content.Shared._Scp.ScpMask;
using Content.Shared._Scp.Watching;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Audio;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System : EntitySystem
{
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedSunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId StatusEffectSleep = "StatusEffectForcedSleeping";

    private static readonly SoundSpecifier StorageOpenSound = new SoundCollectionSpecifier("MetalBreak");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, SimpleEntitySeenEvent>(OnSeen);

        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, AttemptPacifiedAttackEvent>(OnPacifiedAttackAttempt);
        SubscribeLocalEvent<Scp096Component, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<Scp096Component, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp096Component, SleepStateChangedEvent>(OnSleepStateChanged);

        SubscribeLocalEvent<Scp096Component, ScpMaskTargetEquipAttempt>(OnMaskAttempt);

        SubscribeLocalEvent<Scp096Component, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<Scp096Component, ComponentInit>(OnInit);
        SubscribeLocalEvent<Scp096Component, ComponentShutdown>(OnShutdown);

        InitTargets();
    }

    #region Updater

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);
            UpdateScp096(ent);
        }
    }

    private void UpdateScp096(Entity<Scp096Component> ent)
    {
        if (!ent.Comp.InRageMode)
            return;

        if (!ent.Comp.RageStartTime.HasValue)
            return;

        var currentTime = _timing.CurTime;
        var elapsedTime = currentTime - ent.Comp.RageStartTime.Value;

        if (elapsedTime > ent.Comp.RageDuration)
        {
            OnRageTimeExceeded(ent);
        }
    }

    #endregion

    #region Event handlers

    private void OnSeen(Entity<Scp096Component> ent, ref SimpleEntitySeenEvent args)
    {
        TryAddTarget(ent, args.Viewer);
    }

    private void OnMobStateChanged(Entity<Scp096Component> ent, ref MobStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RaiseLocalEvent(ent, new Scp096RequireUpdateVisualsEvent());

        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
    }

    private void OnSleepStateChanged(Entity<Scp096Component> ent, ref SleepStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RaiseLocalEvent(ent, new Scp096RequireUpdateVisualsEvent());
    }

    protected virtual void OnHandleState(Entity<Scp096Component> ent, ref AfterAutoHandleStateEvent args)
    {

    }

    protected virtual void OnInit(Entity<Scp096Component> ent, ref ComponentInit args)
    {

    }

    protected virtual void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var entityUid, out var targetComponent))
        {
            targetComponent.TargetedBy.Remove(ent.Owner);
            Dirty(entityUid, targetComponent);

            if (targetComponent.TargetedBy.Count == 0)
            {
                RemComp<Scp096TargetComponent>(entityUid);
            }
        }
    }

    private void OnPacifiedAttackAttempt(Entity<Scp096Component> ent, ref AttemptPacifiedAttackEvent args)
    {
        args.Reason = Loc.GetString("scp096-non-agro-attack-attempt");
        args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        if (!CanAttack(ent, target))
        {
            args.Cancel();
            return;
        }

        // Открываем ударом шкафчики и хранилища
        if (TryComp<EntityStorageComponent>(target, out var entityStorageComponent) && !entityStorageComponent.Open)
        {
            _lock.TryUnlock(target, ent);
            _entityStorage.OpenStorage(target, entityStorageComponent);
            _audio.PlayLocal(StorageOpenSound, ent, ent);
        }
    }

    private void OnMaskAttempt(Entity<Scp096Component> ent, ref ScpMaskTargetEquipAttempt args)
    {
        if (ent.Comp.InRageMode)
            args.Cancel();
    }

    #endregion

    #region Targets

    public bool TryAddTarget(EntityUid target, bool ignoreAngle = false, bool ignoreMask = false)
    {
        if (!_helpers.TryGetFirst<Scp096Component>(out var scp))
            return false;

        if (!TryAddTarget(scp.Value, target, ignoreAngle, ignoreMask))
            return false;

        return true;
    }

    public bool TryAddTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false, bool ignoreMask = false)
    {
        if (!CanBeAggro(scp, ignoreMask))
            return false;

        if (!IsValidTarget(scp, target, ignoreAngle))
            return false;

        AddTarget(scp, target);

        return true;
    }

    protected virtual void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        scp.Comp.Targets.Add(target);

        var scpTarget = EnsureComp<Scp096TargetComponent>(target);
        scpTarget.TargetedBy.Add(scp);

        Dirty(target, scpTarget);
        Dirty(scp);

        if (!scp.Comp.InRageMode)
            MakeAngry(scp);
    }

    protected virtual void RemoveTarget(Entity<Scp096Component> scp, Entity<Scp096TargetComponent?> target, bool removeComponent = true)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        scp.Comp.Targets.Remove(target);
        target.Comp.TargetedBy.Remove(scp);

        Dirty(target);
        Dirty(scp);

        if (target.Comp.TargetedBy.Count == 0 && removeComponent)
            RemComp<Scp096TargetComponent>(target);

        if (scp.Comp.Targets.Count == 0)
            Pacify(scp);
    }

    private void RemoveAllTargets(Entity<Scp096Component> ent)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var targetUid, out _))
        {
            RemoveTarget(ent, targetUid);
        }
    }

    #endregion

    private void OnRageTimeExceeded(Entity<Scp096Component> ent)
    {
        RemoveAllTargets(ent);
    }

    #region Helpers

    private bool CanBeAggro(Entity<Scp096Component> ent, bool ignoreMask = false)
    {
        if (HasComp<SleepingComponent>(ent))
            return false;

        if (_mobState.IsIncapacitated(ent))
            return false;

        // В маске мы мирные
        if (_scpMask.HasScpMask(ent) && !ignoreMask)
            return false;

        return true;
    }

    private bool IsValidTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false)
    {
        if (scp.Comp.Targets.Contains(target))
            return false;

        // Проверяем, может ли цель видеть 096. Без учета поля зрения
        if (!_watching.IsWatchedBy(scp, [target], viewers: out _, false))
            return false;

        // Проверяем, есть ли у цели защита от 096
        if (TryComp<Scp096ProtectionComponent>(target, out var protection) && !_random.ProbForEntity(scp, protection.ProblemChance))
            return false;

        // Проверяем, смотрит ли 096 на цель и цель на 096
        if (!IsTargetSeeScp096(target, scp, ignoreAngle))
            return false;

        // Если все условия выполнены, то цель валидна
        return true;
    }

    private void RefreshSpeedModifiers(Entity<Scp096Component> ent)
    {
        var newSpeed = ent.Comp.InRageMode ? ent.Comp.RageSpeed : ent.Comp.BaseSpeed;
        _speedModifier.ChangeBaseSpeed(ent, newSpeed, newSpeed, 20.0f);
    }

    private bool IsTargetSeeScp096(EntityUid viewer, Entity<Scp096Component> scp, bool ignoreAngle)
    {
        // Если игнорируем угол, то считаем, что смотрящий видит 096
        if (ignoreAngle)
            return true;

        // Проверяем, смотрит ли 096 в лицо цели
        if (!_fov.IsInViewAngle(scp.Owner, scp.Comp.ArgoAngle, Angle.Zero, viewer))
            return false;

        // Проверяем, смотри ли цель в лицо 096
        if (!_fov.IsInViewAngle(viewer, scp.Comp.ArgoAngle, Angle.Zero, scp.Owner))
            return false;

        // Соответственно если обе проверки прошли, то цель видит 096
        return true;
    }

    /// <summary>
    /// Проверяет, может ли скромник ударить эту сущность.
    /// Можно ударить свою цель, структуру, борга или животного.
    /// </summary>
    private bool CanAttack(Entity<Scp096Component> scp, EntityUid target)
    {
        if (!scp.Comp.InRageMode)
            return false;

        // Если цель не имеет компонента моргания, значит это 99% не игрок
        // И скромник имеет право это расхуярить(это структура, борг или животное)
        if (!HasComp<BlinkableComponent>(target))
            return true;

        if (!TryComp<Scp096TargetComponent>(target, out var targetComp))
            return false;

        if (!targetComp.TargetedBy.Contains(scp))
            return false;

        return true;
    }

    #endregion

    #region Rage handlers

    private void Pacify(Entity<Scp096Component> ent)
    {
        EnsureComp<PacifiedComponent>(ent);

        ent.Comp.InRageMode = false;
        ent.Comp.RageStartTime = null;
        Dirty(ent);

        _ambientSound.SetSound(ent, ent.Comp.CrySound);
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, ent.Comp.PacifiedTime);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(false));
        RaiseLocalEvent(ent, new Scp096RequireUpdateVisualsEvent());

        RefreshSpeedModifiers(ent);
    }

    private void MakeAngry(Entity<Scp096Component> ent)
    {
        RemComp<PacifiedComponent>(ent);

        ent.Comp.InRageMode = true;
        ent.Comp.RageStartTime = _timing.CurTime;
        Dirty(ent);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(true));
        RaiseLocalEvent(ent, new Scp096RequireUpdateVisualsEvent());

        _ambientSound.SetSound(ent, ent.Comp.RageSound);

        RefreshSpeedModifiers(ent);
    }

    #endregion

    #region Rage-mode door handling

    private void OnCollide(Entity<Scp096Component> ent, ref StartCollideEvent args)
    {
        if (!TryComp<DoorComponent>(args.OtherEntity, out var doorComponent))
            return;

        HandleDoorCollision(ent, new Entity<DoorComponent>(args.OtherEntity, doorComponent));
    }

    protected virtual void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity) {}

    #endregion
}

public sealed class Scp096RageChangedEvent(bool inRage) : EntityEventArgs
{
    public readonly bool InRage = inRage;
}

public sealed class Scp096RequireUpdateVisualsEvent : EntityEventArgs;
