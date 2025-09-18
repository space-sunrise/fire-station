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
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedSunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId StatusEffectSleep = "StatusEffectForcedSleeping";

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

        if (elapsedTime.TotalSeconds > ent.Comp.RageDuration)
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
        UpdateVisualState(ent);

        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
    }

    private void OnSleepStateChanged(Entity<Scp096Component> ent, ref SleepStateChangedEvent args)
    {
        UpdateVisualState(ent);
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

    protected virtual void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        if (!args.Target.HasValue)
            return;

        if (!TryComp<Scp096TargetComponent>(args.Target.Value, out var targetComponent)
            || !targetComponent.TargetedBy.Contains(ent.Owner))
        {
            args.Cancel();
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
        {
            Logger.Debug($"CanBeAggro: {CanBeAggro(scp, ignoreMask)}");
            return false;
        }

        if (!IsValidTarget(scp, target, ignoreAngle))
        {
            Logger.Debug($"IsValidTarget: {IsValidTarget(scp, target, ignoreAngle)}");
            return false;
        }

        AddTarget(scp, target);

        return true;
    }

    protected virtual void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        scp.Comp.Targets.Add(target);

        var scpTarget = EnsureComp<Scp096TargetComponent>(target);
        scpTarget.TargetedBy.Add(scp);

        foreach (var uid in scp.Comp.Targets)
        {
            Logger.Debug($"{Name(uid)}");
        }

        Dirty(target, scpTarget);
        Dirty(scp);

        if (!scp.Comp.InRageMode)
            MakeAngry(scp);

        foreach (var uid in scp.Comp.Targets)
        {
            Logger.Debug($"{Name(uid)}");
        }
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

    private bool IsValidTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false)
    {
        if (scp.Comp.Targets.Contains(target))
        {
            Logger.Debug($"IsValidTarget 1: {scp.Comp.Targets.Contains(target)}");
            return false;
        }

        // Проверяем, может ли цель видеть 096. Без учета поля зрения
        if (!_watching.IsWatchedBy(scp, [target], viewers: out _, false))
        {
            Logger.Debug($"IsValidTarget 2: {_watching.IsWatchedBy(scp, [target], viewers: out _, false)}");
            return false;
        }

        // Проверяем, есть ли у цели защита от 096
        if (TryComp<Scp096ProtectionComponent>(target, out var protection) && !_random.Prob(protection.ProblemChance))
        {
            Logger.Debug($"IsValidTarget 3: false");
            return false;
        }

        // Проверяем, смотрит ли 096 на цель и цель на 096
        if (!IsTargetSeeScp096(target, scp, ignoreAngle))
        {
            Logger.Debug($"IsValidTarget 4: {IsTargetSeeScp096(target, scp, ignoreAngle)}");
            return false;
        }

        // Если все условия выполнены, то цель валидна
        return true;
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
        {
            Logger.Debug($"CanBeAggro 1: {HasComp<SleepingComponent>(ent)}");
            return false;
        }

        if (_mobState.IsIncapacitated(ent))
        {
            Logger.Debug($"CanBeAggro 2: {_mobState.IsIncapacitated(ent)}");
            return false;
        }

        // В маске мы мирные
        if (_scpMask.HasScpMask(ent) && !ignoreMask)
        {
            Logger.Debug($"CanBeAggro 3: {_scpMask.HasScpMask(ent) && !ignoreMask}");
            return false;
        }

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

    #endregion

    #region Rage handlers

    private void Pacify(Entity<Scp096Component> ent)
    {
        EnsureComp<PacifiedComponent>(ent);

        ent.Comp.InRageMode = false;
        ent.Comp.RageStartTime = null;
        Dirty(ent);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(false));

        _ambientSound.SetSound(ent, ent.Comp.CrySound);
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, TimeSpan.FromSeconds(ent.Comp.PacifiedTime));

        RefreshSpeedModifiers(ent);
    }

    private void MakeAngry(Entity<Scp096Component> ent)
    {
        RemComp<PacifiedComponent>(ent);

        ent.Comp.InRageMode = true;
        ent.Comp.RageStartTime = _timing.CurTime;
        Dirty(ent);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(true));

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

    protected virtual void UpdateVisualState(Entity<Scp096Component> ent) {}

}

public sealed class Scp096RageChangedEvent(bool inRage) : EntityEventArgs
{
    public readonly bool InRage = inRage;
}
